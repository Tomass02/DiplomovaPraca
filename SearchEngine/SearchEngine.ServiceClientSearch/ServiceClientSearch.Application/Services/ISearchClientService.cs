using ServiceClientSearch.Domain.Models;

namespace ServiceClientSearch.Application.Services;

public sealed record ClientSearchRequestModel(
    string? KlientId,
    string? Cnum,
    string? Ico,
    string? RodneCislo,
    string? Meno,
    string? Priezvisko,
    string? Mesto,
    string? Psc,
    string? TelCislo,
    string? Email,
    string? Ifo,
    int? Page,
    int? PageSize);

public interface IClientSearchRepository
{
    Task<ClientSearchResultPage> SearchAsync(ClientSearchCriteria criteria, CancellationToken cancellationToken);
}

public interface ISearchClientService
{
    Task<ClientSearchResultPage> SearchAsync(ClientSearchRequestModel request, CancellationToken cancellationToken);
}

public class IClientSearchService(IClientSearchRepository repository) : ISearchClientService
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public Task<ClientSearchResultPage> SearchAsync(ClientSearchRequestModel request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var page = request.Page.GetValueOrDefault(DefaultPage);
        if (page < 1)
        {
            page = DefaultPage;
        }

        var pageSize = request.PageSize.GetValueOrDefault(DefaultPageSize);
        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }

        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        var criteria = new ClientSearchCriteria(
            Normalize(request.KlientId),
            Normalize(request.Cnum),
            Normalize(request.Ico),
            Normalize(request.RodneCislo),
            Normalize(request.Meno),
            Normalize(request.Priezvisko),
            Normalize(request.Mesto),
            Normalize(request.Psc),
            Normalize(request.TelCislo),
            Normalize(request.Email),
            Normalize(request.Ifo),
            page,
            pageSize);

        return repository.SearchAsync(criteria, cancellationToken);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
