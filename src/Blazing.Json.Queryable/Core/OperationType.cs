namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Type of query operation.
/// </summary>
public enum OperationType
{
    /// <summary>Where filter.</summary>
    Where,
    /// <summary>Select projection.</summary>
    Select,
    /// <summary>SelectMany projection.</summary>
    SelectMany,
    /// <summary>OfType filter.</summary>
    OfType,
    /// <summary>OrderBy sorting.</summary>
    OrderBy,
    /// <summary>ThenBy sorting.</summary>
    ThenBy,
    /// <summary>Reverse operation.</summary>
    Reverse,
    /// <summary>Skip paging.</summary>
    Skip,
    /// <summary>Take paging.</summary>
    Take,
    /// <summary>Set operation (Distinct, Union, etc.).</summary>
    SetOperation,
    /// <summary>GroupBy operation.</summary>
    GroupBy,
    /// <summary>Join operation.</summary>
    Join,
    /// <summary>Partitioning operation (TakeLast, SkipLast, TakeWhile, SkipWhile, Chunk).</summary>
    Partitioning,
    /// <summary>Sequence operation (Append, Prepend, Concat, Zip).</summary>
    SequenceOperation,
    /// <summary>DefaultIfEmpty operation.</summary>
    DefaultIfEmpty
}