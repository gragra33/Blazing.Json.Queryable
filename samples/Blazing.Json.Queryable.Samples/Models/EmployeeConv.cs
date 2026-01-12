namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// Employee model for conversion demos.
/// </summary>
public record EmployeeConv
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Department { get; init; } = string.Empty;
    public decimal Salary { get; init; }
}
