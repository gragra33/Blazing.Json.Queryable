namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Types of sequence operations.
/// </summary>
public enum SequenceOperationType
{
    /// <summary>Append operation.</summary>
    Append,
    /// <summary>Prepend operation.</summary>
    Prepend,
    /// <summary>Concat operation.</summary>
    Concat,
    /// <summary>Zip operation.</summary>
    Zip
}