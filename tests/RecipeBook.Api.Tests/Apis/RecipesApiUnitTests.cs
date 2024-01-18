﻿using IdGen;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Api.Apis;
using RecipeBook.Api.Data;
using RecipeBook.Api.Models;

namespace RecipeBook.Api.Tests.Apis;

public sealed class RecipesApiUnitTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RecipesApi.Services _services;

    public RecipesApiUnitTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        ApplicationDbContext context = new(contextOptions);
        context.Database.EnsureCreated();

        context.Recipes.AddRange(RecipesApiTestData.Recipes);
        context.SaveChanges();

        var epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        IdGenerator idGenerator = new(0, new IdGeneratorOptions(timeSource: new DefaultTimeSource(epoch)));

        _services = new RecipesApi.Services(context, idGenerator);
    }

    public void Dispose()
    {
        _services.Context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async void GetListAsyncReturnsRecipes()
    {
        // Act
        var result = await RecipesApi.GetListAsync(_services, null, null);

        // Assert
        Assert.IsType<Ok<PagedResult<Recipe>>>(result.Result);
        var okResult = (Ok<PagedResult<Recipe>>)result.Result;
        Assert.NotNull(okResult.Value);

        Assert.NotEmpty(okResult.Value.Items);
    }

    [Fact]
    public async void GetListAsyncReturnsRecipesForValidTop()
    {
        // Act
        var result = await RecipesApi.GetListAsync(_services, 3, null);

        // Assert
        Assert.IsType<Ok<PagedResult<Recipe>>>(result.Result);
        var okResult = (Ok<PagedResult<Recipe>>)result.Result;
        Assert.NotNull(okResult.Value);

        Assert.Equal(3, okResult.Value.Items.Count());
    }

    [Fact]
    public async void GetListAsyncReturnsValidationProblemForInvalidTop()
    {
        // Act
        var result = await RecipesApi.GetListAsync(_services, 0, null);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async void GetListAsyncReturnsRecipesForLast()
    {
        // Act
        var result = await RecipesApi.GetListAsync(_services, null, 6462258523668480);

        // Assert
        Assert.IsType<Ok<PagedResult<Recipe>>>(result.Result);
        var okResult = (Ok<PagedResult<Recipe>>)result.Result;
        Assert.NotNull(okResult.Value);

        Assert.Multiple(
            () => Assert.NotEmpty(okResult.Value.Items),
            () => Assert.DoesNotContain(okResult.Value.Items, a => a.Id >= 6462258523668480),
            () => Assert.Equal(6462160192405504, okResult.Value.Items.FirstOrDefault()?.Id));
    }

    [Fact]
    public async void GetAsyncReturnsRecipeForValidId()
    {
        // Act
        var result = await RecipesApi.GetAsync(_services, 6462160192405504);

        // Assert
        Assert.IsType<Ok<Recipe>>(result.Result);
        var okResult = (Ok<Recipe>)result.Result;
        Assert.NotNull(okResult.Value);

        Assert.Multiple(
            () => Assert.Equal(6462160192405504, okResult.Value.Id),
            () => Assert.Equal("Test Recipe 2", okResult.Value.Name),
            () => Assert.Equal("Test", okResult.Value.Cuisine),
            () => Assert.Equal("This is a test.", okResult.Value.Description),
            () => Assert.Equal(new DateTime(638412046990521543, DateTimeKind.Utc), okResult.Value.Created),
            () => Assert.Null(okResult.Value.Modified),
            () =>
            {
                Assert.NotNull(okResult.Value.Ingredients);
                Assert.Collection(okResult.Value.Ingredients,
                    x => Assert.Equal("1 tsp of test ingredient 1", x),
                    x => Assert.Equal("1 cup of test ingredient 2", x));
            },
            () => Assert.Equal("This is a test.", okResult.Value.Instructions?.Markdown),
            () => Assert.Equal("<p>This is a test.</p>\n", okResult.Value.Instructions?.Html));
    }

    [Fact]
    public async void GetAsyncReturnsNotFoundForInvalidId()
    {
        // Act
        var result = await RecipesApi.GetAsync(_services, 123);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async void PostAsyncReturnsRecipeForValidModel()
    {
        // Arrange
        CreateOrUpdateRecipeDto newRecipe = new()
        {
            Name = "New Recipe",
            Cuisine = "New",
            Description = "This is a new recipe.",
            Ingredients =
            [
                "1 lb of new ingredient 1",
                "3 oz of new ingredient 2"
            ],
            Instructions = "This is new."
        };

        var utcNow = DateTime.UtcNow;
        var currentId = _services.IdGenerator.CreateId();

        // Act
        var result = await RecipesApi.PostAsync(_services, newRecipe);

        // Assert
        Assert.IsType<Created<Recipe>>(result.Result);
        var createdResult = (Created<Recipe>)result.Result;
        Assert.NotNull(createdResult.Value);

        Assert.Equal($"/api/v1/recipes/{createdResult.Value.Id}", createdResult.Location);

        Assert.Multiple(
            () => Assert.True(currentId < createdResult.Value.Id),
            () => Assert.Equal(newRecipe.Name, createdResult.Value.Name),
            () => Assert.Equal(newRecipe.Description, createdResult.Value.Description),
            () => Assert.True(utcNow < createdResult.Value.Created),
            () => Assert.Null(createdResult.Value.Modified),
            () =>
            {
                Assert.NotNull(createdResult.Value.Ingredients);
                Assert.Collection(createdResult.Value.Ingredients,
                    x => Assert.Equal("1 lb of new ingredient 1", x),
                    x => Assert.Equal("3 oz of new ingredient 2", x));
            },
            () => Assert.Equal(newRecipe.Instructions, createdResult.Value.Instructions?.Markdown),
            () => Assert.Equal("<p>This is new.</p>\n", createdResult.Value.Instructions?.Html));
    }

    [Fact]
    public async void PostAsyncReturnsValidationProblemForInvalidModel()
    {
        // Act
        var result = await RecipesApi.PostAsync(_services, new CreateOrUpdateRecipeDto());

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async void PutAsyncReturnsArticleForValidModel()
    {
        // Arrange
        CreateOrUpdateRecipeDto updatedRecipe = new()
        {
            Name = "Updated Recipe",
            Cuisine = "Updated",
            Description = "This is an updated recipe.",
            Ingredients =
            [
                "2 tbsp of updated ingredient 1",
                "3 updated ingredient 2"
            ],
            Instructions = "This is updated."
        };

        var utcNow = DateTime.UtcNow;

        // Act
        var result = await RecipesApi.PutAsync(_services, 6462318867120128, updatedRecipe);

        // Assert
        Assert.IsType<Ok<Recipe>>(result.Result);
        var okResult = (Ok<Recipe>)result.Result;
        Assert.NotNull(okResult.Value);

        Assert.Multiple(
            () => Assert.Equal(6462318867120128, okResult.Value.Id),
            () => Assert.Equal(updatedRecipe.Name, okResult.Value.Name),
            () => Assert.Equal(updatedRecipe.Description, okResult.Value.Description),
            () => Assert.Equal(new DateTime(638412047368832961), okResult.Value.Created),
            () => Assert.NotNull(okResult.Value.Modified),
            () => Assert.True(utcNow < okResult.Value.Modified),
            () =>
            {
                Assert.NotNull(okResult.Value.Ingredients);
                Assert.Collection(okResult.Value.Ingredients,
                    x => Assert.Equal("2 tbsp of updated ingredient 1", x),
                    x => Assert.Equal("3 updated ingredient 2", x));
            },
            () => Assert.Equal(updatedRecipe.Instructions, okResult.Value.Instructions?.Markdown),
            () => Assert.Equal("<p>This is updated.</p>\n", okResult.Value.Instructions?.Html));
    }

    [Fact]
    public async void PutAsyncReturnsValidationProblemForInvalidModel()
    {
        // Act
        var result = await RecipesApi.PutAsync(_services, 6462318867120128, new CreateOrUpdateRecipeDto());

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async void PutAsyncReturnsNotFoundForInvalidId()
    {
        // Arrange
        CreateOrUpdateRecipeDto updatedRecipe = new()
        {
            Name = "Updated Recipe",
            Cuisine = "Updated",
            Description = "This is an updated recipe.",
            Ingredients =
            [
                "2 tbsp of updated ingredient 1",
                "3 updated ingredient 2"
            ],
            Instructions = "This is updated."
        };

        // Act
        var result = await RecipesApi.PutAsync(_services, 123, updatedRecipe);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async void DeleteAsyncReturnsNoContentForValidId()
    {
        // Act
        var result = await RecipesApi.DeleteAsync(_services, 6461870173061120);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async void DeleteAsyncReturnsNotFoundForInvalidId()
    {
        // Act
        var result = await RecipesApi.DeleteAsync(_services, 123);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }
}
