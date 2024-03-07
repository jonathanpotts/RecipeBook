﻿using JonathanPotts.RecipeCatalog.Application;
using JonathanPotts.RecipeCatalog.WebApi.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JonathanPotts.RecipeCatalog.WebApi.Shared.Tests;

public sealed class ExtensionsUnitTests
{
    [Fact]
    public void AddWebApiServicesReturnsPopulatedServiceProvider()
    {
        // Arrange
        ServiceCollection services = new();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        // Act
        services.AddWebApiServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<IProblemDetailsService>());
    }

    [Fact]
    public void AddDbMigratorReturnsPopulatedServiceProvider()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        services.AddSingleton(configuration);
        services.AddApplicationServices(configuration);
        services.AddIdentityApiEndpoints();

        // Act
        services.AddDbMigrator();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetService<DbMigrator>());
    }
}
