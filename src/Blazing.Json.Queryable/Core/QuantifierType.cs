namespace Blazing.Json.Queryable.Core;

/// <summary>
/// Types of quantifier operations.
/// </summary>
public enum QuantifierType
{
    /// <summary>All operation.</summary>
    All,
    /// <summary>Any operation.</summary>
    Any,
    /// <summary>Contains operation.</summary>
    Contains,
    /// <summary>SequenceEqual operation.</summary>
    SequenceEqual
}