using FluentValidation;
using ServiceClientSearch.Application.Features.SearchClient;

namespace ServiceClientSearch;

[ServiceContract]
public interface IServiceClientSearch
{
    [OperationContract]
    Task<ClientSearchResponse> SearchClients(ClientSearchRequest request);
}

public class ServiceClientSearch(SearchClientHandler searchClientHandler) : IServiceClientSearch
{
    public async Task<ClientSearchResponse> SearchClients(ClientSearchRequest request)
    {
        try
        {
            return await searchClientHandler.HandleAsync(request);
        }
        catch (ValidationException ex)
        {
            var detail = string.Join(", ", ex.Errors.Select(static error => error.ErrorMessage));
            throw new FaultException(detail);
        }
    }
}
