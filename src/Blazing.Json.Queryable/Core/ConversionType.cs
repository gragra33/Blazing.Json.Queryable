namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Types of conversion operations.
/// </summary>
public enum ConversionType
{
    /// <summary>ToArray operation.</summary>
    ToArray,
    /// <summary>ToList operation.</summary>
    ToList,
    /// <summary>ToDictionary operation.</summary>
    ToDictionary,
    /// <summary>ToHashSet operation.</summary>
    ToHashSet,
    /// <summary>ToLookup operation.</summary>
    ToLookup
}