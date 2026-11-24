using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.Services;
using CatchTheSun.Data;
using CatchTheSun.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<SiteService>();
builder.Services.AddScoped<IVariantRepository, VariantRepository>();
builder.Services.AddScoped<VariantService>();
builder.Services.AddScoped<ICriterionRepository, CriterionRepository>();
builder.Services.AddScoped<CriterionService>();
builder.Services.AddScoped<ICriterionValueRepository, CriterionValueRepository>();
builder.Services.AddScoped<CriterionValueService>();
builder.Services.AddScoped<ICalculationRepository, CalculationRepository>();
builder.Services.AddScoped<ICalculationService, CalculationService>();
builder.Services.AddScoped<ICalculationAlgorithm, AhpWeightedSumAlgorithm>();
builder.Services.AddScoped<ICalculationAlgorithm, TopsisAlgorithm>();
builder.Services.AddScoped<ICalculationAlgorithm, ParetoAlgorithm>();
builder.Services.AddScoped<ICalculationAlgorithmFactory, CalculationAlgorithmFactory>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "CatchTheSun API v1"));
}

app.MapControllers();

app.Run();
