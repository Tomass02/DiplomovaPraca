namespace SearchEngine.DatabaseDeploy;

internal static class Generator
{
    private static readonly string[] MaleNames =
    [
        "Tomáš", "Peter", "Martin", "Ján", "Marek", "Lukáš", "Michal", "Andrej", "Samuel", "Dominik",
        "Patrik", "Matej", "Filip", "Adam", "Juraj", "Róbert", "Viktor", "Richard", "Štefan", "Pavol"
    ];

    private static readonly string[] FemaleNames =
    [
        "Anna", "Petra", "Mária", "Lucia", "Jana", "Katarína", "Zuzana", "Monika", "Eva", "Simona",
        "Natália", "Veronika", "Barbora", "Nina", "Kristína", "Lenka", "Michaela", "Daniela", "Ivana", "Alena"
    ];

    private static readonly string[] LastNames =
    [
        "Hodas", "Novák", "Kováč", "Horváth", "Bartoš", "Krištofík", "Tóth", "Varga", "Baláž", "Polák",
        "Molnár", "Hudák", "Németh", "Benko", "Šimko", "Kováčik", "Oravec", "Mikula", "Král", "Černý"
    ];

    private static readonly string[] Cities = ["Žilina", "Bratislava", "Košice", "Nitra", "Trenčín", "Banská Bystrica"];

    private static readonly string[] Streets = ["Hlavná", "Jarná", "Dlhá", "Mierová", "Školská", "Lesná", "Slnečná", "Krátka"];
    private static readonly string[] AddressTypes = ["Z", "Y"];

    public static IReadOnlyList<KlientData> GenerateKlienti(int count = 1)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

        if (count > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(count), "Maximum supported count is 1,000,000 because cnum is 6 digits.");

        var result = new List<KlientData>(count);
        var usedKlientIds = new HashSet<string>(StringComparer.Ordinal);
        var usedCnums = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < count; i++)
            result.Add(GenerateKlient(usedKlientIds, usedCnums));

        return result;
    }

    private static KlientData GenerateKlient(HashSet<string> usedKlientIds, HashSet<string> usedCnums)
    {
        var isMale = Random.Shared.Next(0, 2) == 0;
        var firstName = isMale
            ? MaleNames[Random.Shared.Next(MaleNames.Length)]
            : FemaleNames[Random.Shared.Next(FemaleNames.Length)];

        var lastName = LastNames[Random.Shared.Next(LastNames.Length)];
        var city = Cities[Random.Shared.Next(Cities.Length)];
        var gender = isMale ? "M" : "F";

        var birthDate = DateOnly.FromDateTime(DateTime.Today)
            .AddYears(-Random.Shared.Next(18, 80))
            .AddDays(Random.Shared.Next(0, 365));

        var firstContactDate = birthDate.AddYears(18);
        if (firstContactDate > DateOnly.FromDateTime(DateTime.Today))
            firstContactDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-Random.Shared.Next(1, 365));

        var klientId = GenerateUniqueDigits(10, usedKlientIds);
        var cnum = GenerateUniqueDigits(6, usedCnums);
        var rodneCislo = BuildRodneCislo(birthDate, isMale);
        var dokladCislo = BuildDokladCislo();
        var createdAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 800));
        var changedAt = createdAt.AddDays(Random.Shared.Next(1, 90)).AddMinutes(Random.Shared.Next(1, 1440));
        var expiryDate = DateOnly.FromDateTime(DateTime.Today).AddYears(Random.Shared.Next(1, 10));

        return new KlientData(
            KlientId: klientId,
            Cnum: cnum,
            Meno: firstName,
            Priezvisko: lastName,
            RodneCislo: rodneCislo,
            DatumNarodenia: birthDate,
            DatumPrvehoKontaktu: firstContactDate,
            DokladCislo: dokladCislo,
            Pohlavie: gender,
            MiestoNarodenia: city,
            DatumZapisu: createdAt,
            DatumPoslednejZmeny: changedAt,
            DatumExspiracieDokladu: expiryDate);
    }

    public static List<KlientAdresaData> GenerateAddres(KlientData klient, int count = 1)
    {
        ArgumentNullException.ThrowIfNull(klient);

        if (count <= 0)
            return new List<KlientAdresaData>();

        var addresses = new List<KlientAdresaData>(count);

        for (var i = 0; i < count; i++)
            addresses.Add(GenerateAddressForKlient(klient));

        return addresses;
    }

    private static KlientAdresaData GenerateAddressForKlient(KlientData klient)
    {
        ArgumentNullException.ThrowIfNull(klient);

        var street = Streets[Random.Shared.Next(Streets.Length)];
        var city = Cities[Random.Shared.Next(Cities.Length)];
        var houseNumber = Random.Shared.Next(1, 300).ToString();
        var psc = $"{Random.Shared.Next(10, 999):000} {Random.Shared.Next(10, 99):00}";
        var addressType = AddressTypes[Random.Shared.Next(AddressTypes.Length)];

        return new KlientAdresaData(
            KlientId: klient.KlientId,
            Cnum: klient.Cnum,
            KlientAdresaTyp: addressType,
            Meno: klient.Meno,
            Priezvisko: klient.Priezvisko,
            Nazov: $"{klient.Meno} {klient.Priezvisko}",
            Ulica: street,
            Cislo: houseNumber,
            Psc: psc,
            Mesto: city,
            StatKodTyp: "SK",
            SposobPrebVypisu: "NE",
            ZobrazitAdresuNaVypise: 1,
            PohlavieTyp: klient.Pohlavie,
            AdresaPrePk: 0,
            Status: 0,
            DatumZapisu: klient.DatumZapisu,
            Zapisal: "999611",
            DatumPoslednejZmeny: klient.DatumPoslednejZmeny,
            ZapisalPoslednuZmenu: "999611",
            StavSpracovania: 0,
            MidasGuid: Guid.NewGuid());
    }

    private static string GenerateUniqueDigits(int length, HashSet<string> usedValues)
    {
        const int maxAttempts = 10_000;

        for (var i = 0; i < maxAttempts; i++)
        {
            var value = RandomDigits(length);
            if (usedValues.Add(value))
                return value;
        }

        throw new InvalidOperationException($"Unable to generate unique value with length {length} after {maxAttempts} attempts.");
    }

    private static string BuildRodneCislo(DateOnly birthDate, bool isMale)
    {
        var month = isMale ? birthDate.Month : birthDate.Month + 50;
        var prefix = $"{birthDate:yy}{month:00}{birthDate.Day:00}";

        const int maxAttempts = 10_000;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var baseNineDigits = prefix + RandomDigits(3);
            var controlDigit = CalculateControlDigit(baseNineDigits);

            if (controlDigit is null)
                continue;

            var fullNumber = baseNineDigits + controlDigit.Value;
            if (IsMod11Valid(fullNumber))
                return $"{prefix}/{baseNineDigits[6]}{baseNineDigits[7]}{baseNineDigits[8]}{controlDigit.Value}";
        }

        throw new InvalidOperationException("Unable to generate rodné číslo with valid modulo 11 control digit.");
    }

    private static int? CalculateControlDigit(string baseNineDigits)
    {
        for (var k = 0; k <= 9; k++)
        {
            if (IsMod11Valid(baseNineDigits + k))
                return k;
        }

        return null;
    }

    private static bool IsMod11Valid(string tenDigitNumber)
    {
        var oddSum = 0;
        var evenSum = 0;

        for (var i = 0; i < tenDigitNumber.Length; i++)
        {
            var digit = tenDigitNumber[i] - '0';
            if ((i + 1) % 2 == 0)
                evenSum += digit;
            else
                oddSum += digit;
        }

        return (oddSum - evenSum) % 11 == 0;
    }

    private static string BuildDokladCislo()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var first = letters[Random.Shared.Next(letters.Length)];
        var second = letters[Random.Shared.Next(letters.Length)];

        return $"{first}{second}{RandomDigits(6)}";
    }

    private static string RandomDigits(int length)
    {
        Span<char> digits = stackalloc char[length];

        for (var i = 0; i < digits.Length; i++)
            digits[i] = (char)('0' + Random.Shared.Next(0, 10));

        return new string(digits);
    }

    internal sealed record KlientAdresaData(
        string KlientId,
        string Cnum,
        string KlientAdresaTyp,
        string Meno,
        string Priezvisko,
        string Nazov,
        string Ulica,
        string Cislo,
        string Psc,
        string Mesto,
        string StatKodTyp,
        string SposobPrebVypisu,
        int ZobrazitAdresuNaVypise,
        string PohlavieTyp,
        int AdresaPrePk,
        int Status,
        DateTime DatumZapisu,
        string Zapisal,
        DateTime DatumPoslednejZmeny,
        string ZapisalPoslednuZmenu,
        int StavSpracovania,
        Guid MidasGuid);

    internal sealed record KlientData(
        string KlientId,
        string Cnum,
        string Meno,
        string Priezvisko,
        string RodneCislo,
        DateOnly DatumNarodenia,
        DateOnly DatumPrvehoKontaktu,
        string DokladCislo,
        string Pohlavie,
        string MiestoNarodenia,
        DateTime DatumZapisu,
        DateTime DatumPoslednejZmeny,
        DateOnly DatumExspiracieDokladu);
}
