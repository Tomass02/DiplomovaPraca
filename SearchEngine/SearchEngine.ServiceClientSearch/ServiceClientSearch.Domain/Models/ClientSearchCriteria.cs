namespace ServiceClientSearch.Domain.Models;

public sealed record ClientSearchCriteria(
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
    int Page,
    int PageSize);
