using System.Runtime.Serialization;

namespace ServiceClientSearch.Application.Features.SearchClient;

[DataContract]
public record ClientSearchRequest
{
    [DataMember(Order = 1, EmitDefaultValue = false)]
    public string KlientId { get; set; } = string.Empty;

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public string Cnum { get; set; } = string.Empty;

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string Ico { get; set; } = string.Empty;

    [DataMember(Order = 4, EmitDefaultValue = false)]
    public string RodneCislo { get; set; } = string.Empty;

    [DataMember(Order = 5, EmitDefaultValue = false)]
    public string Meno { get; set; } = string.Empty;

    [DataMember(Order = 6, EmitDefaultValue = false)]
    public string Priezvisko { get; set; } = string.Empty;

    [DataMember(Order = 7, EmitDefaultValue = false)]
    public string Mesto { get; set; } = string.Empty;

    [DataMember(Order = 8, EmitDefaultValue = false)]
    public string Psc { get; set; } = string.Empty;

    [DataMember(Order = 9, EmitDefaultValue = false)]
    public string TelCislo { get; set; } = string.Empty;

    [DataMember(Order = 10, EmitDefaultValue = false)]
    public string Email { get; set; } = string.Empty;

    [DataMember(Order = 11, EmitDefaultValue = false)]
    public string Ifo { get; set; } = string.Empty;

    [DataMember(Order = 12, EmitDefaultValue = false)]
    public int? Page { get; set; }

    [DataMember(Order = 13, EmitDefaultValue = false)]
    public int? PageSize { get; set; }
}
