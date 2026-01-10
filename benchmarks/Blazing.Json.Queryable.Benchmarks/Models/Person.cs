namespace Blazing.Json.Queryable.Benchmarks;

/// <summary>
/// Test model representing a person for benchmarks.
/// </summary>
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? City { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
