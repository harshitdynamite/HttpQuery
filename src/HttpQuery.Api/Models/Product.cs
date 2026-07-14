namespace HttpQuery.Api.Models;

public record Product(
    int Id,
    string Name,
    string Category,
    decimal Price,
    string[] Tags,
    string Author);
