using CatchTheSun.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CatchTheSun.Tests.Integration.PostgreSql;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration tests";
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgreSqlFixture.ConnectionStringEnvironmentVariable)))
        {
            Skip = $"Set {PostgreSqlFixture.ConnectionStringEnvironmentVariable} to a dedicated empty PostgreSQL test database to run this integration test.";
        }
    }
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvironmentVariable = "CATCHTHESUN_TEST_POSTGRES";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(configuredConnectionString);
        var databaseName = builder.Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            string.Equals(databaseName, "catchthesun", StringComparison.OrdinalIgnoreCase) ||
            !(databaseName.EndsWith("_test", StringComparison.OrdinalIgnoreCase) ||
              databaseName.EndsWith("_tests", StringComparison.OrdinalIgnoreCase) ||
              databaseName.StartsWith("test_", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Refusing PostgreSQL integration tests: the database name must be a dedicated test database ending in _test/_tests or starting with test_. The configured name '{databaseName}' is not allowed.");
        }

        connectionString = builder.ConnectionString;
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        if (connectionString is null)
        {
            throw new InvalidOperationException($"No PostgreSQL test connection is configured. Set {ConnectionStringEnvironmentVariable}.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    public async Task DisposeAsync()
    {
        // Test rows are inserted inside per-test transactions and rolled back.
        // The fixture deliberately leaves the database and its schema intact.
        await Task.CompletedTask;
    }
}
