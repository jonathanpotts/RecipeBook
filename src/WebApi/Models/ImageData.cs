﻿using System.ComponentModel.DataAnnotations;

namespace JonathanPotts.RecipeCatalog.WebApi.Models;

public class ImageData
{
    [Required]
    public string? Url { get; set; }

    public string? AltText { get; set; }
}