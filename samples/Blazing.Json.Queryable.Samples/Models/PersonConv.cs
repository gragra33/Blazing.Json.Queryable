namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// Person model for conversion demos.
/// </summary>
public record PersonConv
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public string City { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
