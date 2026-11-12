using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceClientSearch.Application.Features.SearchClient;
using ServiceClientSearch.Application.Services;

namespace ServiceClientSearch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddHandlers();
        services.AddServices();
        services.AddValidators();

        return services;
    }

    private static void AddHandlers(this IServiceCollection services) =>
        services.AddTransient<SearchClientHandler>();

    private static void AddServices(this IServiceCollection services) =>
        services.AddTransient<ISearchClientService, IClientSearchService>();

    private static void AddValidators(this IServiceCollection services) =>
        services.AddSingleton<IValidator<ClientSearchRequest>, SearchClientRequestValidator>();
}
