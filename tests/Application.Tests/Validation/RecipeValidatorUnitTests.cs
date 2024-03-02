﻿using FluentValidation.TestHelper;
using JonathanPotts.RecipeCatalog.Application.Validation;
using JonathanPotts.RecipeCatalog.Domain.Entities;

namespace JonathanPotts.RecipeCatalog.Application.Tests.Validation;

public sealed class RecipeValidatorUnitTests
{
    private readonly RecipeValidator _validator = new();

    [Fact]
    public void TestValidateSucceedsWhenValid()
    {
        // Arrange
        Recipe recipe = new()
        {
            Id = 6461870173061120,
            OwnerId = "d7df5331-1c53-491f-8b71-91989846874f",
            Name = "Test",
            CuisineId = 1,
            Ingredients = ["Ingredient 1"],
            Instructions = new()
            {
                Markdown = "This is a test.",
                Html = "<p>This is a test.</p>\n"
            }
        };

        // Act
        var results = _validator.TestValidate(recipe);

        // Assert
        results.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(default(long), "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, null, "Test", 1, (string[])["Ingredient 1"], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "", "Test", 1, (string[])["Ingredient 1"], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", null, 1, (string[])["Ingredient 1"], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "", 1, (string[])["Ingredient 1"], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", default(int), (string[])["Ingredient 1"], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, null, "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])[], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])[""], "This is a test.", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], null, "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], "", "<p>This is a test.</p>\n")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], "This is a test.", null)]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], "This is a test.", "")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], null, null)]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], null, "")]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], "", null)]
    [InlineData(6461870173061120, "d7df5331-1c53-491f-8b71-91989846874f", "Test", 1, (string[])["Ingredient 1"], "", "")]
    public void TestValidateThrowsWhenInvalid(
        long id,
        string? ownerId,
        string? name,
        int cuisineId,
        string[]? ingredients,
        string? instructionsMarkdown,
        string? instructionsHtml)
    {
        // Arrange
        Recipe recipe = new()
        {
            Id = id,
            OwnerId = ownerId,
            Name = name,
            CuisineId = cuisineId,
            Ingredients = ingredients,
        };

        if (instructionsMarkdown != null)
        {
            recipe.Instructions ??= new();
            recipe.Instructions.Markdown = instructionsMarkdown;
        }

        if (instructionsHtml != null)
        {
            recipe.Instructions ??= new();
            recipe.Instructions.Html = instructionsHtml;
        }

        // Act
        var results = _validator.TestValidate(recipe);

        // Arrange
        results.ShouldHaveAnyValidationError();
    }
}
