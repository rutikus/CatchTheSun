using CatchTheSun.Core.Interfaces;
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
