﻿using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RecipeBook.Api.Models;
using RecipeBook.DataGenerator.Models;
using RecipeBook.DataGenerator.Services;
using Spectre.Console;

namespace RecipeBook.DataGenerator;

public class Worker(
    IHostApplicationLifetime applicationLifetime,
    TextGenerator textGenerator,
    ImageGenerator imageGenerator) : BackgroundService
{
    private const int _recipesPerCuisine = 5;
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    private static readonly string[] _cuisineList =
    [
        "American",
        "Japanese",
        "Mexican",
        "Italian",
        "Chinese",
        "Thai",
        "Indian",
        "Spanish",
        "French",
        "Korean",
        "German"
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        applicationLifetime.ApplicationStopped.Register(OnStopped);

        AnsiConsole.Write(new FigletText("Data Generator").Color(Color.DodgerBlue1));

        var grid = new Grid();
        grid.AddColumns(3);
        grid.AddRow("Chat Completions", "[bold]:sparkles:GPT-3.5 Turbo (1106)[/]", "[dim]gpt-35-turbo (1106)[/]");
        grid.AddRow("Text Embeddings", "[bold]:sparkles:Ada (version 2)[/]", "[dim]text-embedding-ada-002[/]");
        grid.AddRow("Image Generation", "[bold]:sparkles:DALL-E 2[/]", "[dim]dalle2[/]");

        var panel = new Panel(grid)
        {
            Header = new PanelHeader("[bold]Azure OpenAI Models Used[/]")
        };

        AnsiConsole.Write(panel);

        var recipes = await GenerateRecipesAsync(stoppingToken);

        var json = JsonSerializer.Serialize(recipes, jsonOptions);
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "data.json"), json, stoppingToken);

        var imageUrls = await GenerateImagesAsync(recipes, stoppingToken);

        AnsiConsole.MarkupLine(":link: Image URL:");
        AnsiConsole.WriteLine(imageUrls.FirstOrDefault() ?? string.Empty);

        applicationLifetime.StopApplication();
    }

    private async Task<List<Cuisine>> GenerateRecipesAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        List<Cuisine> cuisines = [];

        await AnsiConsole.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn()
            ])
            .StartAsync(async ctx =>
            {
                var recipeListTask = ctx.AddTask("Recipe list").IsIndeterminate();
                recipeListTask.StartTask();

                // Start terminal indeterminate progress bar
                AnsiConsole.Write("\x1b]9;4;3;0\x07");

                var recipeList = await textGenerator.GenerateDataFromChatCompletions(
                    new RecipeList
                    {
                        Cuisines =
                        [
                            new CuisineData
                            {
                                Name = "string",
                                Recipes = ["string"]
                            }
                        ]
                    },
                    "You are a helpful assistant that creates recipe lists.",
                    $"Create a list of homemade recipes popular in the United States with {_recipesPerCuisine} recipes each from the following cuisines: {string.Join(", ", _cuisineList)}",
                    cancellationToken);

                recipeListTask.Value = 100.0;
                recipeListTask.StopTask();

                var recipeCount = recipeList?.Cuisines?.SelectMany(x => x.Recipes ?? []).Count() ?? 0;
                var progressIncrement = recipeCount != 0 ? 1.0 / recipeCount * 100.0 : 100.0;
                var currentProgress = 0.0;

                // Start terminal progress bar
                AnsiConsole.Write("\x1b]9;4;1;0\x07");

                List<Task> tasks = [];

                foreach (var cuisine in recipeList?.Cuisines ?? [])
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var task = ctx.AddTask($"{cuisine.Name} recipes");

                        Cuisine newCuisine = new()
                        {
                            Name = cuisine.Name,
                            Recipes = []
                        };

                        foreach (var name in cuisine.Recipes ?? [])
                        {
                            var recipe = await textGenerator.GenerateDataFromChatCompletions(
                                new RecipeData
                                {
                                    CoverImagePrompt =
                                        "descriptive DALL-E prompt for generating image of plated finished recipe",
                                    Description = "string",
                                    Ingredients = ["string"],
                                    InstructionsMarkdown = "string"
                                },
                                "You are a helpful assistant that creates recipes.",
                                $"Create a recipe for {name}.",
                                cancellationToken);

                            if (recipe == null)
                            {
                                task.Increment(1.0 / cuisine.Recipes!.Count * 100.0);
                                continue;
                            }

                            Recipe newRecipe = new()
                            {
                                Name = name,
                                CoverImagePrompt = recipe.CoverImagePrompt,
                                Description = recipe.Description,
                                Ingredients = recipe.Ingredients?.ToArray(),
                                Instructions = new MarkdownData
                                {
                                    Markdown = recipe.InstructionsMarkdown
                                }
                            };

                            newCuisine.Recipes.Add(newRecipe);
                            task.Increment(1.0 / cuisine.Recipes!.Count * 100.0);

                            currentProgress += progressIncrement;

                            // Update terminal progress bar
                            AnsiConsole.Write($"\x1b]9;4;1;{(int)Math.Round(currentProgress)}\x07");
                        }

                        cuisines.Add(newCuisine);

                        task.Value = 100.0;
                        task.StopTask();
                    }));
                }

                await Task.WhenAll(tasks);

                // Stop terminal progress bar
                AnsiConsole.Write("\x1b]9;4;0;0\x07");
            });

        var elapsed = DateTime.UtcNow - startTime;

        AnsiConsole.MarkupLine($":three_o_clock: Elapsed time: {elapsed}");
        AnsiConsole.MarkupLine($":clipboard: Total recipes: {cuisines.SelectMany(x => x.Recipes!).Count()}");

        return cuisines;
    }

    private async Task<List<string>> GenerateImagesAsync(List<Cuisine> recipes, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        List<string> urls = [];

        await AnsiConsole.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn()
            ])
            .StartAsync(async ctx =>
            {
                var recipeListTask = ctx.AddTask("Pad Thai image").IsIndeterminate();
                recipeListTask.StartTask();

                // Start terminal indeterminate progress bar
                AnsiConsole.Write("\x1b]9;4;3;50\x07");

                urls.Add(await imageGenerator.GenerateImageAsync(
                    "a plate of delicious Pad Thai on a wooden table with chopsticks", cancellationToken));

                recipeListTask.Value = 100.0;
                recipeListTask.StopTask();

                // Stop terminal progress bar
                AnsiConsole.Write("\x1b]9;4;0;0\x07");
            });

        var imageElapsed = DateTime.UtcNow - startTime;

        AnsiConsole.MarkupLine($":three_o_clock: Elapsed time: {imageElapsed}");

        return urls;
    }

    private static void OnStopped()
    {
        // Stop terminal progress bar
        AnsiConsole.Write("\x1b]9;4;0;0\x07");
    }
}