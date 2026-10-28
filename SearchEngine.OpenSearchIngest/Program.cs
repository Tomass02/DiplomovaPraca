using OpenSearch.Client;
using OpenSearch.Net;

var openSearchUri = Environment.GetEnvironmentVariable("OPENSEARCH__URI") ?? "http://localhost:9200";
var indexName = Environment.GetEnvironmentVariable("OPENSEARCH__INDEX") ?? "demo-documents";
var username = Environment.GetEnvironmentVariable("OPENSEARCH__USERNAME");
var password = Environment.GetEnvironmentVariable("OPENSEARCH__PASSWORD");

var settings = new ConnectionSettings(new Uri(openSearchUri));

if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
{
    settings = settings.BasicAuthentication(username, password);
}

var client = new OpenSearchClient(settings);

await WaitForOpenSearchAsync(client);
await EnsureIndexAsync(client, indexName);
await SeedDemoDataAsync(client, indexName);

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
            {
                return;
            }

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
        {
            return;
        }

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
                    .Analyzers(an => an
                        .Custom("search_text_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filters("lowercase", "asciifolding")))))
            .Map<DemoDocument>(m => m
                .Properties(p => p
                    .Text(t => t.Name(n => n.RodneCislo).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.Ico).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.KlientId).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.FullName).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.FullAddress).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.Cnum).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.TelCislo).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.Email).Analyzer("search_text_analyzer"))
                    .Text(t => t.Name(n => n.Ifo).Analyzer("search_text_analyzer")))));

        if (createResponse.IsValid)
        {
            return;
        }

        var indexAlreadyExists = string.Equals(createResponse.ServerError?.Error?.Type, "resource_already_exists_exception", StringComparison.Ordinal);
        if (indexAlreadyExists)
        {
            return;
        }

        lastFailure = GetErrorDetails(createResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    throw new InvalidOperationException($"Unable to check or create index '{indexName}' after {maxAttempts} attempts. {lastFailure}");
}

static async Task SeedDemoDataAsync(IOpenSearchClient client, string indexName)
{
    var countResponse = await client.CountAsync<DemoDocument>(c => c.Index(indexName));
    if (!countResponse.IsValid)
    {
        throw new InvalidOperationException($"Unable to read document count for '{indexName}'. {GetErrorDetails(countResponse)}");
    }

    if (countResponse.Count > 0)
    {
        return;
    }

    var documents = new[]
    {
        new DemoDocument
        {
            RodneCislo = "9001011234",
            Ico = "35795146",
            KlientId = "KLI-0001",
            FullName = "Ján Novák",
            FullAddress = "Hlavná 12, Bratislava",
            Cnum = "CNUM-001",
            TelCislo = "+421901111111",
            Email = "jan.novak@example.com",
            Ifo = "IFO-001"
        },
        new DemoDocument
        {
            RodneCislo = "8805055678",
            Ico = "48123456",
            KlientId = "KLI-0002",
            FullName = "Petra Kováčová",
            FullAddress = "Dlhá 45, Košice",
            Cnum = "CNUM-002",
            TelCislo = "+421902222222",
            Email = "petra.kovacova@example.com",
            Ifo = "IFO-002"
        },
        new DemoDocument
        {
            RodneCislo = "9309094321",
            Ico = "51987654",
            KlientId = "KLI-0003",
            FullName = "Marek Horváth",
            FullAddress = "Námestie SNP 7, Banská Bystrica",
            Cnum = "CNUM-003",
            TelCislo = "+421903333333",
            Email = "marek.horvath@example.com",
            Ifo = "IFO-003"
        }
    };

    var bulkResponse = await client.BulkAsync(b => b
        .Index(indexName)
        .Refresh(Refresh.WaitFor)
        .IndexMany(documents));

    if (!bulkResponse.IsValid || bulkResponse.Errors)
    {
        throw new InvalidOperationException($"Unable to seed demo data for '{indexName}'. {GetErrorDetails(bulkResponse) ?? "Bulk operation reported item-level errors."}");
    }
}

static string GetErrorDetails(IResponse response)
{
    return response.ServerError?.ToString()
        ?? response.OriginalException?.Message
        ?? response.DebugInformation
        ?? "Unknown OpenSearch error.";
}

internal sealed class DemoDocument
{
    public string RodneCislo { get; init; } = string.Empty;

    public string Ico { get; init; } = string.Empty;

    public string KlientId { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string FullAddress { get; init; } = string.Empty;

    public string Cnum { get; init; } = string.Empty;

    public string TelCislo { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Ifo { get; init; } = string.Empty;
}
