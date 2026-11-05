using Microsoft.Extensions.DependencyInjection;
using ServiceClientSearch.Application.Abstractions.Configuration;
using ServiceClientSearch.Application.Services;
using ServiceClientSearch.Infrastructure.Configuration;
using ServiceClientSearch.Infrastructure.Repositories;

namespace ServiceClientSearch.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IConfig, Config>();
        services.AddRepositories();

        return services;
    }

    private static void AddRepositories(this IServiceCollection services) =>
        services.AddScoped<IClientSearchRepository, OpenSearchClientSearchRepository>();
}
