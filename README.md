# HttpQuery demo — RFC 10008 in ASP.NET Core (.NET 10)

A three-chapter demo for a short YouTube video about the new HTTP `QUERY`
method (RFC 10008). Everything is controller-based Web API, not minimal APIs.

## The video's story

You're building an admin dashboard for a wholesaler. The ops team pastes in
a list of SKUs to get current prices for their morning report.

- **Chapter 1 (GET)** — small lookups work. The morning report of 1,000
  SKUs blows past Kestrel's 8 KB request-line limit and returns
  `414 URI Too Long`.
- **Chapter 2 (POST)** — put the SKU list in the body. Works, but POST is
  semantically wrong for a read: no caching, no auto-retry.
- **Chapter 3 (QUERY)** — same body as POST, but with the right verb.
  Safe + idempotent + cacheable per RFC 10008.

## What's in the box

```
HttpQuery/
├── HttpQuery.sln
├── src/
│   ├── HttpQuery.Api/               ASP.NET Core Web API (controllers)
│   │   ├── Attributes/
│   │   │   └── HttpQueryAttribute.cs         Custom [HttpQuery] attribute
│   │   ├── Controllers/
│   │   │   ├── ProductsGetController.cs      Chapter 1 — GET
│   │   │   ├── ProductsPostController.cs     Chapter 2 — POST
│   │   │   └── ProductsQueryController.cs    Chapter 3 — QUERY
│   │   ├── Models/                            Products + Prices DTOs
│   │   └── Services/                          In-memory ProductCatalog
│   └── HttpQuery.DemoClient/         Console app calling QUERY via HttpClient
└── demo/
    ├── requests.http                 On-camera demo requests (VS Code / Rider)
    └── script.md                     Minute-by-minute recording script
```

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` should print `10.x`)
- **VS Code** with the "REST Client" extension, or **JetBrains Rider** —
  either can fire the requests in `demo/requests.http` with a single click

## Running the demo

### 1. Start the API

```powershell
cd src\HttpQuery.Api
dotnet run
```

The API listens on `http://localhost:5080`. Open the URL in a browser to
confirm it's up — you should see a text landing page listing the endpoints.

### 2. Fire the demo requests

Open `demo\requests.http` in VS Code or Rider. Each request has a
"Send Request" link above it. Fire them top to bottom for the video flow —
the sequence walks through the three chapters of the lookup story.

The most important request is **1c** — a lookup of 1,000 SKUs
(~9 KB URL). Kestrel's default `MaxRequestLineSize` is 8 KB, so the
request line blows past it and Kestrel returns `414 URI Too Long`
before the controller even runs. That's the video's punchline.

### 3. Run the client (for the "here's HttpClient calling QUERY" beat)

In a second terminal, with the API still running:

```powershell
cd src\HttpQuery.DemoClient
dotnet run
```

You'll see three sections print:

1. Raw `HttpRequestMessage` with `HttpMethod.Query`
2. A one-liner extension: `http.QueryAsJsonAsync<TReq, TRes>(...)`
3. A safe-method retry helper — `QUERY` is retried like `GET`,
   `POST` would not be

## The endpoints at a glance

**Primary story — bulk price lookup:**

| Chapter | Endpoint | Payload | Safe? | Idempotent? | Cacheable? |
|---|---|---|---|---|---|
| 1. GET | `GET /api/v1/products/prices?ids=…` | URL | ✅ | ✅ | ✅ (but blows up past ~800 SKUs) |
| 2. POST | `POST /api/v2/products/prices` | JSON body | ❌ | ❌ | ❌ |
| 3. QUERY | `QUERY /api/v3/products/prices` | JSON body | ✅ | ✅ | ✅ |

**Bonus story — complex filter search** (same three chapters, richer body):

| Chapter | Endpoint |
|---|---|
| 1. GET | `GET /api/v1/products?category=&minPrice=&…` |
| 2. POST | `POST /api/v2/products/search` |
| 3. QUERY | `QUERY /api/v3/products/search` |

## Notes for the recording

- If Kestrel accepts the 1,000-SKU URL on your machine, add
  `builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestLineSize = 4096);`
  to `Program.cs` — that's still realistic since most reverse proxies
  cap the request line around this size in production.
- Don't rip out GET after this demo. Use QUERY where a filter or ID list
  genuinely needs a body; keep GET for anything shareable or bookmarkable.

## References

- [RFC 10008 — The HTTP QUERY Method](https://www.rfc-editor.org/rfc/rfc10008.html)
- [dotnet/aspnetcore #61089 — Support the new QUERY HTTP method](https://github.com/dotnet/aspnetcore/issues/61089)
- [HttpMethod.Query Property (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpmethod.query?view=net-10.0)
