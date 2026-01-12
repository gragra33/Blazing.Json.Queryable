namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// Product model for conversion demos.
/// </summary>
public record ProductConv
{
    public int ProductId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public double Rating { get; init; }
}
