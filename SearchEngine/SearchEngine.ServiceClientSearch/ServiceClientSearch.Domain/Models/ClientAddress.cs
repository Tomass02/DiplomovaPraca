namespace ServiceClientSearch.Domain.Models;

public sealed record ClientAddress(
    string? Ulica,
    string? Cislo,
    string? Psc,
    string? Mesto);
