namespace Blazing.Json.Queryable.Samples.Models;

/// <summary>
/// Common Employee model used across multiple samples.
/// </summary>
public record Employee
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Department { get; init; } = string.Empty;
    public int DepartmentId { get; init; }
    public decimal Salary { get; init; }
    public string City { get; init; } = string.Empty;
    public int Score { get; init; }
    public int YearsEmployed { get; init; }
}
