using HttpQuery.Api.Attributes;
using HttpQuery.Api.Models;
using HttpQuery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HttpQuery.Api.Controllers;

/// <summary>
/// CHAPTER 3 — the "QUERY era" (RFC 10008).
///
/// Same body as the POST version, but the method is QUERY — the RFC
/// declares it BOTH safe AND idempotent. So we get:
///   - No URL length limit (body carries the filter).
///   - Response is cacheable (cache key includes the body).
///   - Retry libraries can safely retry.
///   - Correct semantics: this is a read, not a mutation.
/// </summary>
[ApiController]
[Route("api/v3/products")]
public sealed class ProductsQueryController : ControllerBase
{
    private readonly IProductCatalog _catalog;

    public ProductsQueryController(IProductCatalog catalog) => _catalog = catalog;

    [HttpQuery("search")]
    public ActionResult<ProductSearchResponse> Search([FromBody] ProductSearchRequest request)
    {
        // QUERY is safe + idempotent → cacheable, just like GET.
        Response.Headers.CacheControl = "public, max-age=60";

        var result = _catalog.Search(request);
        return Ok(result);
    }

    /// <summary>
    /// Bulk price lookup, QUERY style. Same body as the POST version,
    /// but declared with the right verb. RFC 10008 says QUERY is safe AND
    /// idempotent — so this response is cacheable, retries are safe, and
    /// the semantics match reality: this is a READ, not a mutation.
    /// </summary>
    [HttpQuery("prices")]
    public ActionResult<PriceLookupResponse> LookupPrices([FromBody] PriceLookupRequest request)
    {
        if (request.Ids is null || request.Ids.Length == 0)
            return BadRequest("ids must be a non-empty array.");

        Response.Headers.CacheControl = "public, max-age=60";

        var result = _catalog.LookupPrices(request.Ids);
        return Ok(result);
    }
}
