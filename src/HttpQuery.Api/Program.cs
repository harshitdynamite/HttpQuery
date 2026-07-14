using HttpQuery.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Full Web API (controller-based, not minimal APIs).
builder.Services.AddControllers();
builder.Services.AddSingleton<IProductCatalog, ProductCatalog>();

var app = builder.Build();

app.MapControllers();

// Tiny landing page so you can see the demo is live in a browser.
app.MapGet("/", () => Results.Text(
    """
    HttpQuery demo API is running.

    -- Bulk price lookup (the video's main story) --
    Chapter 1  GET   /api/v1/products/prices?ids=1&ids=2&...
    Chapter 2  POST  /api/v2/products/prices    body: { "ids": [1,2,...] }
    Chapter 3  QUERY /api/v3/products/prices    body: { "ids": [1,2,...] }

    -- Complex filter (bonus: shows QUERY body expressiveness) --
    Chapter 1  GET   /api/v1/products?category=books&minPrice=10&...
    Chapter 2  POST  /api/v2/products/search
    Chapter 3  QUERY /api/v3/products/search

    Open demo/requests.http in VS Code or Rider to fire the demo requests.
    """));

app.Run();
