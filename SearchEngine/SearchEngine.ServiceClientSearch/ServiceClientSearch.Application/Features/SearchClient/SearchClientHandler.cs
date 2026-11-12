using FluentValidation;
using ServiceClientSearch.Application.Services;
using ServiceClientSearch.Domain.Models;

namespace ServiceClientSearch.Application.Features.SearchClient;

public class SearchClientHandler(
    ISearchClientService searchClientService,
    IValidator<ClientSearchRequest> validator)
{
    public async Task<ClientSearchResponse> HandleAsync(ClientSearchRequest request)
    {
        await validator.ValidateAndThrowAsync(request);

        var resultPage = await searchClientService.SearchAsync(new ClientSearchRequestModel(
            request.KlientId,
            request.Cnum,
            request.Ico,
            request.RodneCislo,
            request.Meno,
            request.Priezvisko,
            request.Mesto,
            request.Psc,
            request.TelCislo,
            request.Email,
            request.Ifo,
            request.Page,
            request.PageSize),
            CancellationToken.None);

        return MapResponse(resultPage);
    }

    private static ClientSearchResponse MapResponse(ClientSearchResultPage resultPage)
    {
        return new ClientSearchResponse
        {
            Items = resultPage.Items.Select(MapClient).ToList(),
            TotalCount = resultPage.TotalCount,
            Page = resultPage.Page,
            PageSize = resultPage.PageSize,
            HasMore = resultPage.HasMore
        };
    }

    private static ClientDto MapClient(ClientSearchItem item)
    {
        return new ClientDto
        {
            KlientId = item.KlientId,
            Cnum = item.Cnum,
            Ico = item.Ico,
            RodneCislo = item.RodneCislo,
            Meno = item.Meno,
            Priezvisko = item.Priezvisko,
            Adresy = item.Adresy.Select(MapAddress).ToArray()
        };
    }

    private static ClientAddressDto MapAddress(ClientAddress address)
    {
        return new ClientAddressDto
        {
            Ulica = address.Ulica,
            Cislo = address.Cislo,
            Psc = address.Psc,
            Mesto = address.Mesto
        };
    }
}
