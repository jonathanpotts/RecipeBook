using System.Text.Json;
using System.Text.Json.Serialization;
using IdGen;
using IdGen.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Api.Apis;
using RecipeBook.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(AppContext.BaseDirectory, $"{nameof(ApplicationDbContext)}.db")}"));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add IdGen for creating Snowflake IDs
var generatorId = builder.Configuration.GetValue("GeneratorId", 0);
var epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
builder.Services.AddIdGen(generatorId, () => new IdGeneratorOptions(timeSource: new DefaultTimeSource(epoch)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapRecipesApi();

app.Run();