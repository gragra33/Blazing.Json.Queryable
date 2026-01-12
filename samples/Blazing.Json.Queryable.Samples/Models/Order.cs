namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// Common Order model used across multiple samples.
/// </summary>
public record Order
{
    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal Total { get; init; }
    public string OrderDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Date { get; init; }
}
