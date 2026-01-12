namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// User preference model for real-world scenarios.
/// </summary>
public record UserPreference
{
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
