using OpenSearch.Client;
using ServiceClientSearch.Application.Abstractions.Configuration;
using ServiceClientSearch.Application.Services;
using ServiceClientSearch.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceClientSearch.Infrastructure.Repositories;

public sealed class OpenSearchClientSearchRepository : IClientSearchRepository
{
    private static readonly string[] KlientIdFields = ["klientId"];
    private static readonly string[] CnumFields = ["cnum"];
    private static readonly string[] IcoFields = ["ico"];
    private static readonly string[] RodneCisloFields = ["rodneCislo"];
    private static readonly string[] MenoFields = ["name"];
    private static readonly string[] PriezviskoFields = ["surname"];
    private static readonly string[] TelCisloFields = ["telCislo"];
    private static readonly string[] EmailFields = ["email"];
    private static readonly string[] IfoFields = ["ifo"];

    private const string NestedAddressPath = "adresy";
    private const string LegacyAddressPath = "klient_adresy";
    private const int MaxResultWindow = 10_000;

    private readonly IOpenSearchClient client;
    private readonly string index;

    public OpenSearchClientSearchRepository(IConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var uri = config.OPEN_SEARCH;
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new InvalidOperationException("OpenSearch URI is not configured.");
        }

        if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            uri = $"http://{uri}";
        }

        index = config.OPEN_SEARCH_INDEX;
        if (string.IsNullOrWhiteSpace(index))
        {
            throw new InvalidOperationException("OpenSearch index is not configured.");
        }

        var settings = new ConnectionSettings(new Uri(uri));
        client = new OpenSearchClient(settings);
    }

    public async Task<ClientSearchResultPage> SearchAsync(ClientSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var from = checked((criteria.Page - 1) * criteria.PageSize);
        if (from + criteria.PageSize > MaxResultWindow)
        {
            throw new InvalidOperationException($"Requested page exceeds max supported window ({MaxResultWindow}).");
        }

        var request = new SearchRequest<OpenSearchClientDocument>(index)
        {
            From = from,
            Size = criteria.PageSize + 1,
            TrackTotalHits = true,
            Query = BuildQuery(criteria)
        };

        var response = await client.SearchAsync<OpenSearchClientDocument>(request, cancellationToken);
        if (!response.IsValid)
        {
            throw new InvalidOperationException(GetErrorDetails(response));
        }

        var mappedItems = response.Hits
            .Select(static hit => MapToDomain(hit.Source))
            .Take(criteria.PageSize)
            .ToArray();

        return new ClientSearchResultPage(
            mappedItems,
            response.Total,
            criteria.Page,
            criteria.PageSize,
            response.Hits.Count > criteria.PageSize);
    }

    private static QueryContainer BuildQuery(ClientSearchCriteria criteria)
    {
        var mustClauses = new List<QueryContainer>();

        AddIdentifierFilter(mustClauses, KlientIdFields, criteria.KlientId);
        AddIdentifierFilter(mustClauses, CnumFields, criteria.Cnum);
        AddIdentifierFilter(mustClauses, IcoFields, criteria.Ico);
        AddIdentifierFilter(mustClauses, RodneCisloFields, criteria.RodneCislo);
        AddIdentifierFilter(mustClauses, TelCisloFields, criteria.TelCislo);
        AddIdentifierFilter(mustClauses, EmailFields, criteria.Email);
        AddIdentifierFilter(mustClauses, IfoFields, criteria.Ifo);

        AddTextFilter(mustClauses, MenoFields, criteria.Meno);
        AddTextFilter(mustClauses, PriezviskoFields, criteria.Priezvisko);

        var addressFilter = BuildAddressFilter(criteria.Mesto, criteria.Psc);
        if (addressFilter is not null)
        {
            mustClauses.Add(addressFilter);
        }

        if (mustClauses.Count == 0)
        {
            return new MatchAllQuery();
        }

        return new BoolQuery { Must = mustClauses };
    }

    private static void AddIdentifierFilter(ICollection<QueryContainer> mustClauses, IEnumerable<string> fields, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmedValue = value.Trim();
        var normalized = trimmedValue.ToLowerInvariant();
        var shouldClauses = new List<QueryContainer>();

        foreach (var field in fields)
        {
            shouldClauses.Add(new TermQuery
            {
                Field = $"{field}.exact",
                Value = trimmedValue
            });

            shouldClauses.Add(new TermQuery
            {
                Field = $"{field}.exact",
                Value = normalized
            });

            shouldClauses.Add(new PrefixQuery
            {
                Field = $"{field}.exact",
                Value = trimmedValue
            });

            shouldClauses.Add(new PrefixQuery
            {
                Field = $"{field}.exact",
                Value = normalized
            });

            shouldClauses.Add(new MatchQuery
            {
                Field = field,
                Query = trimmedValue,
                Operator = Operator.And
            });

            shouldClauses.Add(new MatchPhrasePrefixQuery
            {
                Field = field,
                Query = trimmedValue
            });
        }

        mustClauses.Add(new BoolQuery
        {
            Should = shouldClauses,
            MinimumShouldMatch = 1
        });
    }

    private static void AddTextFilter(ICollection<QueryContainer> mustClauses, IEnumerable<string> fields, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var shouldClauses = new List<QueryContainer>();

        foreach (var field in fields)
        {
            shouldClauses.Add(new MatchQuery
            {
                Field = field,
                Query = value,
                Operator = Operator.And
            });

            shouldClauses.Add(new MatchPhrasePrefixQuery
            {
                Field = field,
                Query = value
            });
        }

        mustClauses.Add(new BoolQuery
        {
            Should = shouldClauses,
            MinimumShouldMatch = 1
        });
    }

    private static QueryContainer? BuildAddressFilter(string? mesto, string? psc)
    {
        if (string.IsNullOrWhiteSpace(mesto) && string.IsNullOrWhiteSpace(psc))
        {
            return null;
        }

        return new NestedQuery
        {
            Path = NestedAddressPath,
            IgnoreUnmapped = true,
            Query = new BoolQuery
            {
                Must = BuildAddressMustClauses(NestedAddressPath, mesto, psc)
            }
        };
    }

    private static List<QueryContainer> BuildAddressMustClauses(string path, string? mesto, string? psc)
    {
        var mustClauses = new List<QueryContainer>();

        if (!string.IsNullOrWhiteSpace(mesto))
        {
            mustClauses.Add(new MatchQuery
            {
                Field = $"{path}.mesto",
                Query = mesto,
                Operator = Operator.And
            });
        }

        if (!string.IsNullOrWhiteSpace(psc))
        {
            mustClauses.Add(new BoolQuery
            {
                Should =
                [
                    new PrefixQuery
                    {
                        Field = $"{path}.psc",
                        Value = psc.ToLowerInvariant()
                    },
                    new MatchPhrasePrefixQuery
                    {
                        Field = $"{path}.psc",
                        Query = psc
                    }
                ],
                MinimumShouldMatch = 1
            });
        }

        return mustClauses;
    }

    private static ClientSearchItem MapToDomain(OpenSearchClientDocument document)
    {
        return new ClientSearchItem(
            document.KlientId,
            document.Cnum,
            document.Ico,
            document.RodneCislo,
            document.Name,
            document.Surname,
            (document.Adresy ?? []).Select(MapAddress).ToArray());
    }

    private static ClientAddress MapAddress(OpenSearchClientAddress address)
    {
        return new ClientAddress(
            address.Ulica,
            address.Cislo,
            address.Psc,
            address.Mesto);
    }

    private static string GetErrorDetails(IResponse response)
    {
        return response.ServerError?.ToString()
            ?? response.OriginalException?.Message
            ?? response.DebugInformation
            ?? "Unknown OpenSearch error.";
    }

    private sealed class OpenSearchClientDocument
    {
        [PropertyName("klientId")]
        public string? KlientId { get; init; }

        [PropertyName("cnum")]
        public string? Cnum { get; init; }

        [PropertyName("ico")]
        public string? Ico { get; init; }

        [PropertyName("rodneCislo")]
        public string? RodneCislo { get; init; }

        [PropertyName("name")]
        public string? Name { get; init; }

        [PropertyName("surname")]
        public string? Surname { get; init; }

        [PropertyName("adresy")]
        public IReadOnlyCollection<OpenSearchClientAddress>? Adresy { get; init; }
    }

    private sealed class OpenSearchClientAddress
    {
        [PropertyName("ulica")]
        public string? Ulica { get; init; }

        [PropertyName("cislo")]
        public string? Cislo { get; init; }

        [PropertyName("psc")]
        public string? Psc { get; init; }

        [PropertyName("mesto")]
        public string? Mesto { get; init; }
    }
}
