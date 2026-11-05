using System.Collections.Generic;

namespace ServiceClientSearch.Domain.Models;

public sealed record ClientSearchResultPage(
    IReadOnlyCollection<ClientSearchItem> Items,
    long TotalCount,
    int Page,
    int PageSize,
    bool HasMore);
