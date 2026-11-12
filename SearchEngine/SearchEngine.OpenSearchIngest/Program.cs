using Microsoft.Extensions.Hosting;
using OpenSearch.Client;
using OpenSearch.Net;

var hostBuilder = Host.CreateApplicationBuilder(args);
hostBuilder.AddServiceDefaults();
using var host = hostBuilder.Build();

var openSearchUri = Environment.GetEnvironmentVariable("OPENSEARCH_URI");
if (string.IsNullOrEmpty(openSearchUri))
    throw new Exception("OPENSEARCH_URI is null!");

var indexName = Environment.GetEnvironmentVariable("OPENSEARCH_INDEX");
var username = Environment.GetEnvironmentVariable("OPENSEARCH_USERNAME");
var password = Environment.GetEnvironmentVariable("OPENSEARCH_PASSWORD");

var settings = new ConnectionSettings(new Uri(openSearchUri));

if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
{
    settings = settings.BasicAuthentication(username, password);
}

var client = new OpenSearchClient(settings);

await WaitForOpenSearchAsync(client);
await EnsureIndexAsync(client, indexName);

Console.WriteLine($"OpenSearch index '{indexName}' is ready with demo data.");

static async Task WaitForOpenSearchAsync(IOpenSearchClient client)
{
    const int maxAttempts = 30;
    string? lastFailure = null;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var pingResponse = await client.PingAsync();
            if (pingResponse.IsValid)
                return;

            lastFailure = GetErrorDetails(pingResponse);
        }
        catch (Exception ex)
        {
            lastFailure = ex.Message;
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    throw new InvalidOperationException($"OpenSearch is not reachable after {maxAttempts} attempts. {lastFailure}");
}

static async Task EnsureIndexAsync(IOpenSearchClient client, string indexName)
{
    const int maxAttempts = 20;
    string? lastFailure = null;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        var existsResponse = await client.Indices.ExistsAsync(indexName);

        if (existsResponse.Exists)
            return;

        var existsStatusCode = existsResponse.ApiCall?.HttpStatusCode;
        var isNotFoundResponse = existsStatusCode == 404 || existsResponse.ServerError?.Status == 404;

        if (!existsResponse.IsValid && !isNotFoundResponse)
        {
            lastFailure = GetErrorDetails(existsResponse);
            await Task.Delay(TimeSpan.FromSeconds(2));
            continue;
        }

        var createResponse = await client.Indices.CreateAsync(indexName, c => c
    .Settings(s => s
        .NumberOfShards(1)
        .NumberOfReplicas(0)
        .Analysis(a => a
            // Token filter pre prefixové vyhľadávanie: zo vstupu vytvára pod-reťazce od 3 do 20 znakov
            .TokenFilters(tf => tf
                .EdgeNGram("identifier_edge_ngram", ng => ng
                    .MinGram(3)
                    .MaxGram(20)))
            .Analyzers(an => an
                // Analyzátor pre bežný text (meno, adresa): delí na slová, zmenšuje písmená a odstraňuje diakritiku
                .Custom("search_text_analyzer", ca => ca
                    .Tokenizer("standard")
                    .Filters("lowercase", "asciifolding"))
                // Analyzátor pre indexovanie identifikátorov: nedelí text, zmenší písmená, odstráni diakritiku a vygeneruje N-Gramy
                .Custom("identifier_index_analyzer", ca => ca
                    .Tokenizer("keyword")
                    .Filters("lowercase", "asciifolding", "identifier_edge_ngram"))
                // Analyzátor pre vyhladavanie v identifikátoroch: nedelí text, zmenší písmená a odstráni diakritiku (bez N-Gramov)
                .Custom("identifier_search_analyzer", ca => ca
                    .Tokenizer("keyword")
                    .Filters("lowercase", "asciifolding")))))
    .Map<Klient>(m => m
        .Properties(p => p
            // Identifikačné a kontaktné polia: N-Gramy pri indexovaní + pod-pole "exact" pre presnú zhodu
            .Text(t => t
                .Name(n => n.RodneCislo)
                .Analyzer("identifier_index_analyzer")
                .SearchAnalyzer("identifier_search_analyzer")
                .Fields(f => f
                    .Keyword(k => k.Name("exact"))))
            .Text(t => t
                .Name(n => n.Ico)
                .Analyzer("identifier_index_analyzer")
                .SearchAnalyzer("identifier_search_analyzer")
                .Fields(f => f
                    .Keyword(k => k.Name("exact"))))
            .Text(t => t
                .Name(n => n.KlientId)
                .Analyzer("identifier_index_analyzer")
                .SearchAnalyzer("identifier_search_analyzer")
                .Fields(f => f
                    .Keyword(k => k.Name("exact"))))
            // Meno a priezvisko: štandardné textové vyhľadávanie bez diakritiky
            .Text(t => t.Name(n => n.Name).Analyzer("search_text_analyzer"))
            .Text(t => t.Name(n => n.Surname).Analyzer("search_text_analyzer"))
            // Zoznam adries klienta (Nested pre správne párovanie podmienok v jednej adrese)
            .Nested<KlientAdresa>(o => o
                .Name(n => n.Adresy)
                .Properties(ap => ap
                    .Text(t => t.Name(a => a.Ulica).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(a => a.Cislo).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(a => a.Psc).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(a => a.Mesto).Analyzer("search_text_analyzer"))
                    .Keyword(k => k.Name(a => a.AdresaTyp))
                    // Kód štátu: presná hodnota bez transformácie
                    .Keyword(k => k.Name(a => a.StatKodTyp))))
            // Ďalšie identifikačné a kontaktné polia s N-Gramy a pod-poľom "exact"
            .Text(t => t
                .Name(n => n.Cnum)
                .Analyzer("identifier_index_analyzer")
                .SearchAnalyzer("identifier_search_analyzer")
                .Fields(f => f
                    .Keyword(k => k.Name("exact"))))
            .Text(t => t
                .Name(n => n.TelCislo)
                .Analyzer("identifier_index_analyzer")
                .SearchAnalyzer("identifier_search_analyzer")
                .Fields(f => f
                    .Keyword(k => k.Name("exact"))))
            .Text(t => t
                .Name(n => n.Email)
                .Analyzer("identifier_index_analyzer")
                .SearchAnalyzer("identifier_search_analyzer")
                .Fields(f => f
                    .Keyword(k => k.Name("exact"))))
            .Text(t => t
                .Name(n => n.Ifo)
                .Analyzer("identifier_index_analyzer")
                .SearchAnalyzer("identifier_search_analyzer")
                .Fields(f => f
                    .Keyword(k => k.Name("exact")))))));

        if (createResponse.IsValid)
            return;

        var indexAlreadyExists = string.Equals(createResponse.ServerError?.Error?.Type, "resource_already_exists_exception", StringComparison.Ordinal);
        if (indexAlreadyExists)
            return;

        lastFailure = GetErrorDetails(createResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    throw new InvalidOperationException($"Unable to check or create index '{indexName}' after {maxAttempts} attempts. {lastFailure}");
}

static string GetErrorDetails(IResponse response)
{
    return response.ServerError?.ToString()
        ?? response.OriginalException?.Message
        ?? response.DebugInformation
        ?? "Unknown OpenSearch error.";
}

internal sealed class Klient
{
    public string RodneCislo { get; init; } = string.Empty;

    public string Ico { get; init; } = string.Empty;

    public string KlientId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Surname { get; init; } = string.Empty;

    public KlientAdresa[] Adresy { get; init; } = [];

    public string Cnum { get; init; } = string.Empty;

    public string TelCislo { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Ifo { get; init; } = string.Empty;
}

internal sealed class KlientAdresa
{
    public string Ulica { get; init; } = string.Empty;

    public string Cislo { get; init; } = string.Empty;

    public string Psc { get; init; } = string.Empty;

    public string Mesto { get; init; } = string.Empty;

    public char AdresaTyp { get; init; }

    public string StatKodTyp { get; init; } = string.Empty;
}
