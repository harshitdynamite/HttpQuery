namespace HttpQuery.Api.Models;

public record ProductSearchResponse(
    IReadOnlyList<Product> Items,
    int Total,
    int Page,
    int PageSize);
