using System.Net.Http.Json;

// -----------------------------------------------------------------------------
// HttpQuery.DemoClient
//
// A tiny console app that shows how a real client calls the QUERY endpoint
// with HttpClient in .NET 10. Story: "give me current prices for these SKUs."
//
// Run the API first (see README), then:
//     cd src/HttpQuery.DemoClient
//     dotnet run
// -----------------------------------------------------------------------------

const string BaseUrl = "http://localhost:5080";

using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };

// A modest lookup — the point of the client demo is the CODE, not blowing
// the URL. Ten SKUs is realistic and prints cleanly.
var request = new PriceLookupRequest(Ids: [1, 2, 3, 10, 11, 12, 13, 14, 15, 1042]);

// -----------------------------------------------------------------------------
// 1) THE RAW WAY — build the HttpRequestMessage yourself.
// -----------------------------------------------------------------------------
Console.WriteLine("=== 1) Raw HttpRequestMessage with HttpMethod.Query ===\n");

using (var msg = new HttpRequestMessage(HttpMethod.Query, "/api/v3/products/prices")
       {
           Content = JsonContent.Create(request),
       })
{
    using var response = await http.SendAsync(msg);
    response.EnsureSuccessStatusCode();

    var prices = await response.Content.ReadFromJsonAsync<PriceLookupResponse>();
    PrintPrices(prices!);
}

// -----------------------------------------------------------------------------
// 2) THE CLEAN WAY — one line, via an extension method.
// -----------------------------------------------------------------------------
Console.WriteLine("\n=== 2) Clean call: http.QueryAsJsonAsync<TReq, TRes>(...) ===\n");

var clean = await http.QueryAsJsonAsync<PriceLookupRequest, PriceLookupResponse>(
    "/api/v3/products/prices", request);

PrintPrices(clean!);

// -----------------------------------------------------------------------------
// 3) THE IDEMPOTENCY PAYOFF — retry safe methods, refuse to retry POST.
//
//    This is the whole reason QUERY exists as a distinct verb. A generic
//    retry helper can now treat QUERY the same way it treats GET.
// -----------------------------------------------------------------------------
Console.WriteLine("\n=== 3) Safe-method retry: QUERY gets retried, POST would not ===\n");

var retried = await http.SendWithSafeRetryAsync(
    method:      HttpMethod.Query,
    requestUri:  "/api/v3/products/prices",
    body:        request,
    maxAttempts: 3);

var retriedPrices = await retried.Content.ReadFromJsonAsync<PriceLookupResponse>();
PrintPrices(retriedPrices!);

Console.WriteLine("\nDone.");


// =============================================================================
// Helpers
// =============================================================================

static void PrintPrices(PriceLookupResponse page)
{
    Console.WriteLine($"  {page.Count} price(s):");
    foreach (var p in page.Prices)
    {
        Console.WriteLine($"    #{p.Id,-6} {p.Name,-30}  ${p.Price,7:0.00}");
    }
}


// =============================================================================
// Extension methods worth putting in a real shared library
// =============================================================================

public static class HttpClientQueryExtensions
{
    /// <summary>
    /// Send a QUERY request with a JSON body and deserialize the JSON response.
    /// The QUERY analogue of PostAsJsonAsync / GetFromJsonAsync.
    /// </summary>
    public static async Task<TResponse?> QueryAsJsonAsync<TRequest, TResponse>(
        this HttpClient client,
        string requestUri,
        TRequest body,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Query, requestUri)
        {
            Content = JsonContent.Create(body),
        };

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }

    /// <summary>
    /// Send any HTTP request. If the method is SAFE (GET or QUERY per RFC 9110
    /// and RFC 10008), transient failures are retried with exponential backoff.
    /// Unsafe methods (POST, PATCH, ...) are sent exactly once — the caller
    /// hasn't proved they're idempotent, so we refuse to retry silently.
    /// </summary>
    public static async Task<HttpResponseMessage> SendWithSafeRetryAsync<TBody>(
        this HttpClient client,
        HttpMethod method,
        string requestUri,
        TBody? body,
        int maxAttempts = 3,
        CancellationToken ct = default)
    {
        // Safe = "can be repeated with no additional effect."
        // In RFC 10008 QUERY joins GET, HEAD, OPTIONS and TRACE in this set.
        var isSafe = HttpMethods.IsSafe(method.Method);
        var attempts = isSafe ? maxAttempts : 1;

        Console.WriteLine(isSafe
            ? $"    -> {method.Method} is SAFE → will retry up to {attempts} times"
            : $"    -> {method.Method} is NOT safe → sending exactly once");

        HttpResponseMessage? last = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var request = new HttpRequestMessage(method, requestUri);
            if (body is not null)
                request.Content = JsonContent.Create(body);

            try
            {
                last = await client.SendAsync(request, ct);
                if ((int)last.StatusCode < 500) return last;
            }
            catch (HttpRequestException) when (attempt < attempts)
            {
                // transient — will retry
            }

            if (attempt < attempts)
            {
                var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
                Console.WriteLine($"    -> attempt {attempt} failed; retrying in {delay.TotalMilliseconds}ms");
                await Task.Delay(delay, ct);
            }
        }
        return last!;
    }
}

/// <summary>
/// Minimal shim: HttpMethods normally lives in Microsoft.AspNetCore.Http,
/// which client apps shouldn't need to reference. Reproducing the tiny
/// bit we use here keeps the client project dependency-free.
/// </summary>
internal static class HttpMethods
{
    public static bool IsSafe(string method) =>
        method.Equals("GET",     StringComparison.OrdinalIgnoreCase) ||
        method.Equals("HEAD",    StringComparison.OrdinalIgnoreCase) ||
        method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) ||
        method.Equals("TRACE",   StringComparison.OrdinalIgnoreCase) ||
        method.Equals("QUERY",   StringComparison.OrdinalIgnoreCase);
}


// =============================================================================
// DTOs — matching the API's contract. In real life you'd share these via a
// contracts package; inlined here so the client project stands alone.
// =============================================================================

public record PriceLookupRequest(int[] Ids);

public record PriceLookupResponse(
    IReadOnlyList<PriceInfo> Prices,
    int Count);

public record PriceInfo(
    int Id,
    string Name,
    decimal Price);
