namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// Common Product model used across multiple samples.
/// </summary>
public record Product
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public double Rating { get; init; }
    public string Code { get; init; } = string.Empty;
}
