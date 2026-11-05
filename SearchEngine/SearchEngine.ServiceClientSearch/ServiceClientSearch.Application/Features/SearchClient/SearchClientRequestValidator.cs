using FluentValidation;
using ServiceClientSearch.Application.Services;

namespace ServiceClientSearch.Application.Features.SearchClient;

internal sealed class SearchClientRequestValidator : AbstractValidator<ClientSearchRequest>
{
    public SearchClientRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .When(request => request.Page.HasValue);

        RuleFor(request => request.PageSize)
            .GreaterThanOrEqualTo(1)
            .When(request => request.PageSize.HasValue);

        RuleFor(request => request.PageSize)
            .LessThanOrEqualTo(IClientSearchService.MaxPageSize)
            .When(request => request.PageSize.HasValue);
    }
}
