using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SearchEngine.DatabaseDeploy;

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

var generateData = Environment.GetEnvironmentVariable("GENERATE_DATA");
var shouldGenerateData = IsTrue(generateData);

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

    if (shouldGenerateData)
    {
        logger.LogInformation("GENERATE_DATA is enabled. Generating and inserting test data.");
        await InsertGeneratedDataAsync(dbContext, logger);
        logger.LogInformation("Generated test data inserted successfully.");
    }
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
    var match = Regex.Match(fileName, @"^(\d+)_");

    return match.Success && int.TryParse(match.Groups[1].Value, out var order)
        ? order
        : int.MaxValue;
}

static bool IsTrue(string? value) =>
    !string.IsNullOrWhiteSpace(value) &&
    (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase));

static async Task InsertGeneratedDataAsync(DatabaseDeployContext dbContext, ILogger logger)
{
    const int klientCount = 1000;

    var klienti = Generator.GenerateKlienti(klientCount);

    foreach (var klient in klienti)
    {
        await InsertKlientAsync(dbContext, klient);

        var addressCount = Random.Shared.Next(0, 3);
        var addresses = Generator.GenerateAddres(klient, addressCount);

        if(addresses.Count > 0)
            foreach (var adresa in addresses)
                await InsertKlientAdresaAsync(dbContext, adresa);
    }

    logger.LogInformation("Inserted {KlientCount} klient records with random 0-2 addresses each.", klientCount);
}

static Task InsertKlientAsync(DatabaseDeployContext dbContext, Generator.KlientData klient)
{
    const string sql = """
INSERT INTO [MAC].[dbo].[KLIENT](
    klient_id, cnum, klient_typ, prospekt_typ, meno, priezvisko, priezvisko_rodne,
    titul_pred_menom, titul_za_menom, nazov_spolocnosti, nazov_spolocnosti_skr, nazov_klienta,
    rodne_cislo, ico, identifikator_klienta, datum_narodenia, datum_zal_spol, datum_umrtia,
    datum_zrus_spol, datum_prveho_kontaktu, doklad_typ, doklad_cislo, pohlavie_typ,
    statna_prislusnost, miesto_narodenia, krajina_narodenia, danovy_domacil, danovy_domacil_datum,
    pravna_forma_typ, dic, kod_obce, nazov_reg_uradu, sidlo_reg_uradu, krajina_reg,
    registracne_cislo, audit_uct_uzavierka, datum_konca_uct_obdobia, ico_zriadovatela,
    nazov_zriadovatela, samosprava_statut_typ, poc_org_v_zriad_posobnosti, rizikova_kategoria,
    rozsah_starostlivosti, insc_kod, indc_kod, ao_id, pobocka_kod, zamestnanec_banky, konkurz,
    zavazky_voci_sp, osobity_vztah, watchlist, blacklist, exekucia, nepovoleny_debet,
    omeskane_splatky, osl_kl_marketingovymi_ponukami, akt_staz_klienta, rating_externy,
    rating_interny, segment, subsegment, rodinny_stav, najvyssie_vzdelanie, preferovany_kontakt,
    pocet_clenov_domacnosti, pocet_vyzivovanych_deti, byvanie, rozdelene_bsm, stav_zamestnanosti,
    sfrb_klient, marketingova_skupina_1, marketingova_skupina_2, marketingova_skupina_3,
    marketingova_skupina_4, marketingova_skupina_5, poznamka, status, datum_zapisu, zapisal,
    datum_poslednej_zmeny, zapisal_poslednu_zmenu, stav_spracovania, midas_guid, rezident_usa,
    datum_exspiracie_dokladu, neobmedzeny_datum_exspiracie_dokladu, tin_udaj, tin, ifo)
VALUES(
    @klient_id, @cnum, 1, 1, @meno, @priezvisko, @priezvisko_rodne,
    NULL, NULL, NULL, NULL, @nazov_klienta,
    @rodne_cislo, NULL, @identifikator_klienta, @datum_narodenia, NULL, NULL,
    NULL, @datum_prveho_kontaktu, 1, @doklad_cislo, @pohlavie_typ,
    'SK', @miesto_narodenia, 'SK', 'SK', NULL,
    NULL, NULL, NULL, NULL, NULL, NULL,
    NULL, NULL, NULL, NULL,
    NULL, NULL, NULL, 1,
    1, '50', NULL, '', 'CEN', 0, 0,
    0, 0, 0, 0, 0, 0,
    0, 1, 0, NULL,
    '', 'RETAIL-OBČANIA', 'MASS', NULL, NULL, NULL,
    NULL, NULL, NULL, 0, NULL,
    0, '', '', 'OBCA',
    '', '', '', 0, @datum_zapisu, '999611',
    @datum_poslednej_zmeny, '999611', 0, NEWID(), 0,
    @datum_exspiracie_dokladu, 0, NULL, NULL, NULL);
""";

    var fullName = $"{klient.Meno} {klient.Priezvisko}";

    return dbContext.Database.ExecuteSqlRawAsync(
        sql,
        new SqlParameter("@klient_id", klient.KlientId),
        new SqlParameter("@cnum", klient.Cnum),
        new SqlParameter("@meno", klient.Meno),
        new SqlParameter("@priezvisko", klient.Priezvisko),
        new SqlParameter("@priezvisko_rodne", klient.Priezvisko),
        new SqlParameter("@nazov_klienta", fullName),
        new SqlParameter("@rodne_cislo", klient.RodneCislo),
        new SqlParameter("@identifikator_klienta", klient.RodneCislo),
        new SqlParameter("@datum_narodenia", klient.DatumNarodenia.ToDateTime(TimeOnly.MinValue)),
        new SqlParameter("@datum_prveho_kontaktu", klient.DatumPrvehoKontaktu.ToDateTime(TimeOnly.MinValue)),
        new SqlParameter("@doklad_cislo", klient.DokladCislo),
        new SqlParameter("@pohlavie_typ", klient.Pohlavie),
        new SqlParameter("@miesto_narodenia", klient.MiestoNarodenia),
        new SqlParameter("@datum_zapisu", klient.DatumZapisu),
        new SqlParameter("@datum_poslednej_zmeny", klient.DatumPoslednejZmeny),
        new SqlParameter("@datum_exspiracie_dokladu", klient.DatumExspiracieDokladu.ToDateTime(TimeOnly.MinValue)));
}

static Task InsertKlientAdresaAsync(DatabaseDeployContext dbContext, Generator.KlientAdresaData adresa)
{
    const string sql = """
INSERT INTO [MAC].[dbo].[KLIENT_ADRESA](
    klient_id, cnum, klient_adresa_typ, meno, priezvisko, titul_pred_menom,
    titul_za_menom, nazov_spolocnosti, nazov, nazov2, ulica, cislo, psc, mesto,
    stat_kod_typ, poznamka, sposob_preb_vypisu, zobrazit_adresu_na_vypise,
    pohlavie_typ, adresa_pre_pk, rozpoctova_organizacia, status, datum_zapisu,
    zapisal, datum_poslednej_zmeny, zapisal_poslednu_zmenu, stav_spracovania,
    midas_guid, miesto_prevadzky)
VALUES(
    @klient_id, @cnum, @klient_adresa_typ, @meno, @priezvisko, NULL,
    NULL, NULL, @nazov, NULL, @ulica, @cislo, @psc, @mesto,
    @stat_kod_typ, NULL, @sposob_preb_vypisu, @zobrazit_adresu_na_vypise,
    @pohlavie_typ, @adresa_pre_pk, NULL, @status, @datum_zapisu,
    @zapisal, @datum_poslednej_zmeny, @zapisal_poslednu_zmenu, @stav_spracovania,
    @midas_guid, NULL);
""";

    return dbContext.Database.ExecuteSqlRawAsync(
        sql,
        new SqlParameter("@klient_id", adresa.KlientId),
        new SqlParameter("@cnum", adresa.Cnum),
        new SqlParameter("@klient_adresa_typ", adresa.KlientAdresaTyp),
        new SqlParameter("@meno", adresa.Meno),
        new SqlParameter("@priezvisko", adresa.Priezvisko),
        new SqlParameter("@nazov", adresa.Nazov),
        new SqlParameter("@ulica", adresa.Ulica),
        new SqlParameter("@cislo", adresa.Cislo),
        new SqlParameter("@psc", adresa.Psc),
        new SqlParameter("@mesto", adresa.Mesto),
        new SqlParameter("@stat_kod_typ", adresa.StatKodTyp),
        new SqlParameter("@sposob_preb_vypisu", adresa.SposobPrebVypisu),
        new SqlParameter("@zobrazit_adresu_na_vypise", adresa.ZobrazitAdresuNaVypise),
        new SqlParameter("@pohlavie_typ", adresa.PohlavieTyp),
        new SqlParameter("@adresa_pre_pk", adresa.AdresaPrePk),
        new SqlParameter("@status", adresa.Status),
        new SqlParameter("@datum_zapisu", adresa.DatumZapisu),
        new SqlParameter("@zapisal", adresa.Zapisal),
        new SqlParameter("@datum_poslednej_zmeny", adresa.DatumPoslednejZmeny),
        new SqlParameter("@zapisal_poslednu_zmenu", adresa.ZapisalPoslednuZmenu),
        new SqlParameter("@stav_spracovania", adresa.StavSpracovania),
        new SqlParameter("@midas_guid", adresa.MidasGuid));
}

internal sealed class DatabaseDeployContext(DbContextOptions<DatabaseDeployContext> options) : DbContext(options);

