namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Types of set operations.
/// </summary>
public enum SetOperationType
{
    /// <summary>Distinct operation.</summary>
    Distinct,
    /// <summary>DistinctBy operation.</summary>
    DistinctBy,
    /// <summary>Union operation.</summary>
    Union,
    /// <summary>UnionBy operation.</summary>
    UnionBy,
    /// <summary>Intersect operation.</summary>
    Intersect,
    /// <summary>IntersectBy operation.</summary>
    IntersectBy,
    /// <summary>Except operation.</summary>
    Except,
    /// <summary>ExceptBy operation.</summary>
    ExceptBy
}