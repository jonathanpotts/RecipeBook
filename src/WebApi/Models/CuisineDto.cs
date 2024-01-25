﻿using System.ComponentModel.DataAnnotations;

namespace JonathanPotts.RecipeCatalog.WebApi.Models;

public class CuisineDto
{
    public int Id { get; set; }

    [Required]
    public string? Name { get; set; }
}
