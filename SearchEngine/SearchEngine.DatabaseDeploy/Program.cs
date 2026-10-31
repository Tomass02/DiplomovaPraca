using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        })
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("DatabaseDeploy");

var connectionString = Environment.GetEnvironmentVariable("DB_DEPLOY_CONNECTION_STRING");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string was not provided. Set DB_DEPLOY_CONNECTION_STRING.");

var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "DbScripts");

if (!Directory.Exists(scriptsDirectory))
    throw new DirectoryNotFoundException($"Scripts directory was not found: {scriptsDirectory}");

var scriptFiles = Directory
    .EnumerateFiles(scriptsDirectory, "*.sql", SearchOption.TopDirectoryOnly)
    .OrderBy(GetScriptOrder)
    .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
    .ToList();

if (scriptFiles.Count == 0)
{
    logger.LogInformation("No SQL script files found. Nothing to do.");
    return;
}

var sqlConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
{
    InitialCatalog = "master"
};

var dbOptions = new DbContextOptionsBuilder<DatabaseDeployContext>()
    .UseSqlServer(sqlConnectionBuilder.ConnectionString, sql => sql.CommandTimeout(180)).Options;

await using var dbContext = new DatabaseDeployContext(dbOptions);

logger.LogInformation("Checking existing database objects.");
if (await SearchEngineDatabaseExistsAsync(dbContext))
{
    logger.LogInformation("Principal 'searchengine' already exists. Deployment will be skipped.");
    return;
}

await dbContext.Database.OpenConnectionAsync();

try
{
    foreach (var scriptFile in scriptFiles)
    {
        var scriptName = Path.GetFileName(scriptFile);
        logger.LogInformation("Executing script: {ScriptName}", scriptName);

        var scriptContent = await File.ReadAllTextAsync(scriptFile);
        var batches = SplitSqlBatches(scriptContent);

        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(batch);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed in file {ScriptName}, batch #{BatchIndex}.", scriptName, i + 1);
                throw new InvalidOperationException($"Failed in file '{scriptName}', batch #{i + 1}.", ex);
            }
        }

        logger.LogInformation("Finished script: {ScriptName}", scriptName);
    }

    logger.LogInformation("Database deployment finished successfully.");
}
finally
{
    await dbContext.Database.CloseConnectionAsync();
}

static async Task<bool> SearchEngineDatabaseExistsAsync(DatabaseDeployContext dbContext)
{
    var exists = await dbContext.Database
        .SqlQueryRaw<int>("SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.server_principals WHERE [name] = N'searchengine') THEN 1 ELSE 0 END AS [Value]")
        .SingleAsync();

    return exists == 1;
}

static List<string> SplitSqlBatches(string sql) => 
    Regex.Split(sql, @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase).ToList();

static int GetScriptOrder(string filePath)
{
    var fileName = Path.GetFileNameWithoutExtension(filePath);
    var match = Regex.Match(fileName, @"^(\d+)");

    return match.Success && int.TryParse(match.Groups[1].Value, out var order)
        ? order
        : int.MaxValue;
}

internal sealed class DatabaseDeployContext(DbContextOptions<DatabaseDeployContext> options) : DbContext(options);

