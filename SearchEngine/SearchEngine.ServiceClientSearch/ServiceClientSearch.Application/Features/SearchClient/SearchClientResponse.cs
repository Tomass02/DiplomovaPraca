using System.Runtime.Serialization;

namespace ServiceClientSearch.Application.Features.SearchClient;

[DataContract]
public record ClientSearchResponse
{
    [DataMember(Order = 1)]
    public List<ClientDto> Items { get; set; } = [];

    [DataMember(Order = 2)]
    public long TotalCount { get; set; }

    [DataMember(Order = 3)]
    public int Page { get; set; }

    [DataMember(Order = 4)]
    public int PageSize { get; set; }

    [DataMember(Order = 5)]
    public bool HasMore { get; set; }
}

[DataContract]
public class ClientDto
{
    [DataMember(Order = 1, EmitDefaultValue = false)]
    public string? KlientId { get; set; }

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public string? Cnum { get; set; }

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string? Ico { get; set; }

    [DataMember(Order = 4, EmitDefaultValue = false)]
    public string? RodneCislo { get; set; }

    [DataMember(Order = 5, EmitDefaultValue = false)]
    public string? Meno { get; set; }

    [DataMember(Order = 6, EmitDefaultValue = false)]
    public string? Priezvisko { get; set; }

    [DataMember(Order = 7)]
    public ClientAddressDto[] Adresy { get; set; } = [];
}

[DataContract]
public class ClientAddressDto
{
    [DataMember(Order = 1, EmitDefaultValue = false)]
    public string? Ulica { get; set; }

    [DataMember(Order = 2, EmitDefaultValue = false)]
    public string? Cislo { get; set; }

    [DataMember(Order = 3, EmitDefaultValue = false)]
    public string? Psc { get; set; }

    [DataMember(Order = 4, EmitDefaultValue = false)]
    public string? Mesto { get; set; }
}