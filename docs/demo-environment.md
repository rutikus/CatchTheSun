# Изолированная БД для демонстрации CatchTheSun

Инструкция создаёт отдельную БД `catchthesun_demo` и направляет API и EF CLI на неё через переменную окружения. `appsettings.json` и строка по умолчанию не меняются. Пароль не сохраняется в Git и не вводится в команду, записываемую в shell history.

## Что проверено в проекте

- API получает строку из `ConnectionStrings:DefaultConnection`; значения из переменных окружения перекрывают `appsettings.json`.
- `appsettings.json` по умолчанию указывает на `catchthesun`, порт `5432`. `appsettings.Development.json` не переопределяет connection string.
- Launch profile `http` запускает Development API на `http://localhost:5126`; profile `https` использует `https://localhost:7177` и `http://localhost:5126`.
- Swagger UI включён только в Development; UI — `/swagger`, OpenAPI JSON — `/openapi/v1.json`.
- Приложение само не вызывает `Migrate`, `EnsureCreated` или `EnsureDeleted`. Схему нужно обновить явной командой EF CLI.
- В проекте три миграции: `20261124134048_InitialCreate`, `20261124175506_StrengthenDataIntegrity`, `20261124222039_CalculationEngineSnapshots`. Локальный manifest закрепляет `dotnet-ef` версии `10.0.15`.
- EF-модель содержит таблицы участков, вариантов, критериев, значений критериев, расчётов, критериев расчёта, сравнений, результатов и snapshots значений результатов.

При проверке окружения локальный PostgreSQL 18 cluster `18/main` обнаружен на порту `5433`, но сейчас имеет статус **down**. Поэтому live-схему и статус миграций существующих баз проверить не удалось. Команды ниже сначала требуют запустить кластер, а затем создают/используют только `catchthesun_demo`.

В выводе `pg_lsclusters` владелец кластера также отображался как `nobody`. Если запуск кластера завершится ошибкой или владелец останется `nobody`, остановитесь: не меняйте владельца каталога данных, не переинициализируйте кластер и не обходите эту ошибку. Сначала необходимо отдельно выяснить, почему установлен такой владелец.

## 1. Запустить PostgreSQL и проверить наличие demo-БД

В терминале проекта:

```bash
pg_lsclusters
sudo pg_ctlcluster 18 main start
pg_lsclusters
```

Убедитесь, что `18/main` имеет статус `online` и порт `5433`. Затем проверьте, существует ли БД:

```bash
sudo -u postgres psql -p 5433 -d postgres -Atc \
  "SELECT datname FROM pg_database WHERE datname = 'catchthesun_demo';"
```

Если запрос ничего не вывел, создайте БД. Если вывел `catchthesun_demo`, не создавайте её повторно и сначала убедитесь, что это именно демонстрационная база, данные которой можно использовать.

```bash
sudo -u postgres createdb -p 5433 -O postgres catchthesun_demo
```

Команды не удаляют и не пересоздают существующие БД и не меняют пароль роли.

## 2. Временно направить EF CLI и API на demo-БД

Оставьте эту же shell-сессию открытой для команд миграции и запуска API. Введите **уже существующий** пароль PostgreSQL роли `postgres` в скрытом prompt. Пароль не попадёт в историю команд; не включайте `set -x` и не выводите значение переменной.

```bash
export ASPNETCORE_ENVIRONMENT=Development
read -r -s -p 'Existing PostgreSQL password for postgres: ' CATCHTHESUN_DEMO_PASSWORD
printf '\n'
export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5433;Database=catchthesun_demo;Username=postgres;Password=${CATCHTHESUN_DEMO_PASSWORD}"
unset CATCHTHESUN_DEMO_PASSWORD
```

Проверьте, что переменная нацелена на demo-БД, не печатая пароль:

```bash
printf '%s\n' "$ConnectionStrings__DefaultConnection" \
  | sed -E 's/(Password=)[^;]*/\1<redacted>/I'
```

В строке должно быть `Database=catchthesun_demo`, `Port=5433`; пароль должен отображаться как `<redacted>`. Если видите `catchthesun` или `catchthesun_test`, остановитесь и не запускайте миграции.

## 3. Проверить соединение и миграции

`psql` проверяет выбранную БД напрямую и запросит пароль интерактивно:

```bash
psql -h 127.0.0.1 -p 5433 -U postgres -d catchthesun_demo -W -X \
  -c 'SELECT current_database(), current_user;'
```

Результат должен содержать `catchthesun_demo`. Затем восстановите локальный EF CLI и посмотрите список миграций до обновления:

```bash
dotnet tool restore
dotnet ef migrations list \
  --context ApplicationDbContext \
  --project CatchTheSun.Data/CatchTheSun.Data.csproj \
  --startup-project CatchTheSun.API/CatchTheSunAPI.csproj
```

Примените миграции только после повторной проверки connection string:

```bash
dotnet ef database update \
  --context ApplicationDbContext \
  --project CatchTheSun.Data/CatchTheSun.Data.csproj \
  --startup-project CatchTheSun.API/CatchTheSunAPI.csproj
```

Проверьте `__EFMigrationsHistory` в demo-БД:

```bash
psql -h 127.0.0.1 -p 5433 -U postgres -d catchthesun_demo -W -X \
  -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'
```

Ожидаются три миграции, перечисленные выше. Таблицы можно проверить командой `\dt` в `psql`, подключённом к `catchthesun_demo`.

## 4. Запустить API и открыть Swagger

В той же shell-сессии, пока действует временная переменная подключения:

```bash
dotnet run --project CatchTheSun.API/CatchTheSunAPI.csproj --launch-profile http
```

Откройте `http://localhost:5126/swagger`. Перед созданием данных выполните `GET /api/sites`: в новой пустой demo-БД ожидается `200 OK` и пустой список. Дополнительно `psql` из предыдущего шага подтверждает имя БД независимо от содержимого.

Выполните запросы из [демонстрационного сценария](calculation-demo-scenario.md), подставляя GUID из ответов Swagger. Данные автоматически не создаются; seed endpoint не добавлен.

## 5. Вернуться к обычной конфигурации

Остановите API `Ctrl+C`. Для восстановления настроек, заданных в `appsettings.json`, удалите временные overrides в demo-терминале:

```bash
unset ConnectionStrings__DefaultConnection
unset ASPNETCORE_ENVIRONMENT
```

Можно вместо этого закрыть demo-терминал и открыть новую shell-сессию. Файл `appsettings.json` остаётся без изменений и продолжает задавать прежнюю строку по умолчанию. Если кластер нужен другим локальным задачам, оставьте его запущенным; не выполняйте команды удаления БД.
