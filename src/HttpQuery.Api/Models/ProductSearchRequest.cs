namespace HttpQuery.Api.Models;

/// <summary>
/// The rich filter model that the POST and QUERY endpoints accept as a JSON body.
/// The GET endpoint uses a flattened version of the same fields via [FromQuery].
/// </summary>
public record ProductSearchRequest(
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string[]? Tags = null,
    string[]? Authors = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 25);
