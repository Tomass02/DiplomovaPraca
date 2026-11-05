using Microsoft.Extensions.Configuration;
using ServiceClientSearch.Application.Abstractions.Configuration;

namespace ServiceClientSearch.Infrastructure.Configuration;

public class Config(IConfiguration config) : IConfig
{
    private readonly IConfigurationSection _connectionStrings = config.GetSection("ConnectionStrings");
    private readonly IConfigurationSection _openSearchSection = config.GetSection("OpenSearch");

    public string OPEN_SEARCH =>
        _openSearchSection.GetValue<string>("Uri")
        ?? _connectionStrings.GetValue<string>("OPEN_SEARCH")
        ?? throw new InvalidOperationException("OpenSearch URI is not configured.");

    public string OPEN_SEARCH_INDEX =>
        _openSearchSection.GetValue<string>("Index")
        ?? "klient";
}