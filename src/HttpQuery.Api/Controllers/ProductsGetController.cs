using HttpQuery.Api.Models;
using HttpQuery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HttpQuery.Api.Controllers;

/// <summary>
/// CHAPTER 1 — the "GET era".
///
/// The classic approach: every filter is a query-string parameter.
/// - Safe + idempotent + cacheable. Good.
/// - Complex filters bloat the URL fast, and URL length is capped
///   (Kestrel's default MaxRequestLineSize is 8 KB — cross it and
///   the client sees 414 URI Too Long before this controller runs).
/// </summary>
[ApiController]
[Route("api/v1/products")]
public sealed class ProductsGetController : ControllerBase
{
    private readonly IProductCatalog _catalog;

    public ProductsGetController(IProductCatalog catalog) => _catalog = catalog;

    [HttpGet]
    public ActionResult<ProductSearchResponse> Search([FromQuery] ProductSearchRequest request)
    {
        // GET is cacheable by design — advertise it so caches / CDNs cooperate.
        Response.Headers.CacheControl = "public, max-age=60";

        var result = _catalog.Search(request);
        return Ok(result);
    }

    /// <summary>
    /// Bulk price lookup, GET style. Every SKU is a repeated ?ids=… query
    /// parameter. Works fine for a handful — but once the ops team pastes
    /// their morning report (~1,000 SKUs), the request line blows past
    /// Kestrel's 8 KB MaxRequestLineSize and this action never even runs
    /// — Kestrel replies with 414 URI Too Long first.
    /// </summary>
    [HttpGet("prices")]
    public ActionResult<PriceLookupResponse> LookupPrices([FromQuery] int[] ids)
    {
        if (ids is null || ids.Length == 0)
            return BadRequest("ids query parameter is required (e.g. ?ids=1&ids=2).");

        Response.Headers.CacheControl = "public, max-age=60";

        var result = _catalog.LookupPrices(ids);
        return Ok(result);
    }
}
