using HttpQuery.Api.Models;

namespace HttpQuery.Api.Services;

/// <summary>
/// In-memory seeded catalog. Not the point of the demo — just realistic enough
/// that filters return sensible results on camera.
/// </summary>
public sealed class ProductCatalog : IProductCatalog
{
    private static readonly Product[] Seed =
    [
        new(1,  "The Foundation Trilogy",     "books",       25.00m, ["fiction",  "scifi"],     "asimov"),
        new(2,  "Dune",                       "books",       19.99m, ["fiction",  "scifi"],     "herbert"),
        new(3,  "The Left Hand of Darkness",  "books",       15.50m, ["fiction",  "scifi"],     "leguin"),
        new(4,  "Nightfall",                  "books",        9.99m, ["fiction",  "scifi"],     "asimov"),
        new(5,  "Dune Messiah",               "books",       17.25m, ["fiction",  "scifi"],     "herbert"),
        new(6,  "The Dispossessed",           "books",       18.00m, ["fiction",  "scifi"],     "leguin"),
        new(7,  "A Wizard of Earthsea",       "books",       12.75m, ["fiction",  "fantasy"],   "leguin"),
        new(8,  "The Silent Patient",         "books",       14.50m, ["fiction",  "thriller"],  "michaelides"),
        new(9,  "Gone Girl",                  "books",       13.25m, ["fiction",  "thriller"],  "flynn"),
        new(10, "Clean Architecture",         "books",       32.00m, ["nonfiction", "tech"],    "martin"),
        new(11, "Domain-Driven Design",       "books",       48.50m, ["nonfiction", "tech"],    "evans"),
        new(12, "Sapiens",                    "books",       21.99m, ["nonfiction", "history"], "harari"),
        new(13, "Mechanical Keyboard",        "electronics", 89.00m, ["peripheral"],            "n/a"),
        new(14, "USB-C Hub",                  "electronics", 34.50m, ["peripheral"],            "n/a"),
        new(15, "Studio Headphones",          "electronics", 199.00m,["audio"],                 "n/a"),
    ];

    public ProductSearchResponse Search(ProductSearchRequest request)
    {
        IEnumerable<Product> q = Seed;

        if (!string.IsNullOrWhiteSpace(request.Category))
            q = q.Where(p => string.Equals(p.Category, request.Category, StringComparison.OrdinalIgnoreCase));

        if (request.MinPrice is { } min)
            q = q.Where(p => p.Price >= min);

        if (request.MaxPrice is { } max)
            q = q.Where(p => p.Price <= max);

        if (request.Tags is { Length: > 0 } tags)
            q = q.Where(p => tags.Any(t => p.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)));

        if (request.Authors is { Length: > 0 } authors)
            q = q.Where(p => authors.Contains(p.Author, StringComparer.OrdinalIgnoreCase));

        q = request.Sort switch
        {
            "price"        => q.OrderBy(p => p.Price),
            "-price"       => q.OrderByDescending(p => p.Price),
            "name"         => q.OrderBy(p => p.Name),
            "-name"        => q.OrderByDescending(p => p.Name),
            _              => q.OrderBy(p => p.Id),
        };

        var materialised = q.ToList();
        var total    = materialised.Count;
        var page     = request.Page     < 1 ? 1  : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;
        var items    = materialised.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new ProductSearchResponse(items, total, page, pageSize);
    }

    /// <summary>
    /// Bulk price lookup — the "give me prices for these SKUs" endpoint that
    /// the video story is built around. Real IDs (1–15) resolve to the seeded
    /// catalog; synthetic IDs get a deterministic name + price so the response
    /// is populated regardless of what the client sent.
    /// </summary>
    public PriceLookupResponse LookupPrices(IReadOnlyList<int> ids)
    {
        var byId = Seed.ToDictionary(p => p.Id);
        var results = new List<PriceInfo>(ids.Count);

        foreach (var id in ids)
        {
            if (byId.TryGetValue(id, out var real))
            {
                results.Add(new PriceInfo(real.Id, real.Name, real.Price));
            }
            else
            {
                // Deterministic synthetic pricing so re-runs look identical.
                var price = 5.00m + (decimal)(id % 100) * 0.99m;
                results.Add(new PriceInfo(id, $"SKU-{id:00000}", Math.Round(price, 2)));
            }
        }

        return new PriceLookupResponse(results, results.Count);
    }
}
