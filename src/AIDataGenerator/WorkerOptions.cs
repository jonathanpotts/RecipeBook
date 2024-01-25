﻿using System.ComponentModel.DataAnnotations;

namespace JonathanPotts.RecipeCatalog.AIDataGenerator;

internal class WorkerOptions
{
    [Required]
    public List<string>? Cuisines { get; set; }

    [Range(1, int.MaxValue)]
    public int? RecipesPerCuisine { get; set; }

    [Range(1, int.MaxValue)]
    public int? RecipeGenerationMaxConcurrency { get; set; }

    [Range(1, int.MaxValue)]
    public int? ImageGenerationMaxConcurrency { get; set; }

    [Range(0, 100)]
    public int? ImageQuality { get; set; }
}
