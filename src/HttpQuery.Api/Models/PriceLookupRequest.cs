namespace HttpQuery.Api.Models;

/// <summary>
/// A batch price-lookup request — "give me current prices for these SKUs."
/// The POST and QUERY endpoints take this as a JSON body.
/// The GET endpoint takes the same Ids as repeated ?ids=… query params.
/// </summary>
public record PriceLookupRequest(int[] Ids);
