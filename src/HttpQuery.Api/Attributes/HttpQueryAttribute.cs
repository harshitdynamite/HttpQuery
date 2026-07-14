using Microsoft.AspNetCore.Mvc.Routing;

namespace HttpQuery.Api.Attributes;

/// <summary>
/// Marks a controller action to respond to the HTTP QUERY method (RFC 10008).
///
/// What .NET 10 does ship (via aspnetcore#63260, merged Aug 2025, RC1):
///   - HttpMethods.Query   — the string constant "QUERY"
///   - HttpMethods.IsQuery — the boolean check for middleware
///
/// What it does NOT ship (yet): the MVC [HttpQuery] attribute and the
/// minimal-API MapQuery extension were explicitly held back pending
/// community feedback. Until they land, this ten-line shim on top of
/// HttpMethodAttribute is how you use QUERY from a controller today.
///
/// Note we reference HttpMethods.Query rather than a magic string, so
/// the day Microsoft ship [HttpQuery] we can delete this file and swap
/// the attribute over with no other changes.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HttpQueryAttribute : HttpMethodAttribute
{
    // Fully qualified: the base HttpMethodAttribute has its own instance
    // member called `HttpMethods`, which would shadow the framework's
    // static class of the same name and give CS0236 in this initializer.
    private static readonly IEnumerable<string> SupportedMethods =
        [Microsoft.AspNetCore.Http.HttpMethods.Query];

    public HttpQueryAttribute()
        : base(SupportedMethods) { }

    public HttpQueryAttribute(string template)
        : base(SupportedMethods, template) { }
}
