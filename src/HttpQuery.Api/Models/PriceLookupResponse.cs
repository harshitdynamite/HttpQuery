namespace HttpQuery.Api.Models;

public record PriceLookupResponse(
    IReadOnlyList<PriceInfo> Prices,
    int Count);

public record PriceInfo(
    int Id,
    string Name,
    decimal Price);
