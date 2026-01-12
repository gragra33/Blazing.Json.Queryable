namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// Common Person model used across multiple samples.
/// </summary>
public record Person
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public string City { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string Department { get; init; } = string.Empty;
    public int Score { get; init; }
}
