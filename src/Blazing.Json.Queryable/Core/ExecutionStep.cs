namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Represents a single operation step in query execution.
/// </summary>
public sealed class ExecutionStep
{
    /// <summary>
    /// Type of operation in this step.
    /// </summary>
    public required OperationType OperationType { get; init; }

    /// <summary>
    /// Delegate for this operation (predicates, selectors, etc.).
    /// </summary>
    public Delegate? Delegate { get; init; }

    /// <summary>
    /// Additional data needed for this operation (comparers, sequences, etc.).
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// For partitioning operations (Skip, Take, TakeLast, SkipLast, Chunk).
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Result type after this operation.
    /// </summary>
    public Type? ResultType { get; init; }
}