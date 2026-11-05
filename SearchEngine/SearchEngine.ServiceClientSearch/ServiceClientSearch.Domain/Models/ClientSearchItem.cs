using System.Collections.Generic;

namespace ServiceClientSearch.Domain.Models;

public sealed record ClientSearchItem(
    string? KlientId,
    string? Cnum,
    string? Ico,
    string? RodneCislo,
    string? Meno,
    string? Priezvisko,
    IReadOnlyCollection<ClientAddress> Adresy);
