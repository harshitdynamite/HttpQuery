using HttpQuery.Api.Models;
using HttpQuery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HttpQuery.Api.Controllers;

/// <summary>
/// CHAPTER 2 — the "POST workaround".
///
/// We move the filter into a JSON body to escape the URL-length problem.
/// It works, but semantically it's wrong:
///   - POST is defined as unsafe and non-idempotent (RFC 9110 §9.3.3).
///   - Browsers, CDNs and shared caches will NOT cache POST responses.
///   - Retry libraries (Polly, browser fetch, load balancers) refuse to
///     auto-retry POST because they can't tell it's a read.
///
/// We deliberately do NOT set Cache-Control here — even if we did,
/// intermediaries would ignore it for POST.
/// </summary>
[ApiController]
[Route("api/v2/products")]
public sealed class ProductsPostController : ControllerBase
{
    private readonly IProductCatalog _catalog;

    public ProductsPostController(IProductCatalog catalog) => _catalog = catalog;

    [HttpPost("search")]
    public ActionResult<ProductSearchResponse> Search([FromBody] ProductSearchRequest request)
    {
        var result = _catalog.Search(request);
        return Ok(result);
    }

    /// <summary>
    /// Bulk price lookup, POST style. The SKU list lives in the JSON body,
    /// so length is no longer a problem — but semantically we've lied about
    /// what this endpoint does. Reviewers see POST and assume mutation.
    /// Caches and retry libraries refuse to touch it.
    /// </summary>
    [HttpPost("prices")]
    public ActionResult<PriceLookupResponse> LookupPrices([FromBody] PriceLookupRequest request)
    {
        if (request.Ids is null || request.Ids.Length == 0)
            return BadRequest("ids must be a non-empty array.");

        var result = _catalog.LookupPrices(request.Ids);
        return Ok(result);
    }
}
