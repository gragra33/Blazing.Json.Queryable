using Blazing.Json.Queryable.Utilities;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Utilities;

/// <summary>
/// Tests for <see cref="JoinDetectionHelper"/> to verify correct Join vs GroupJoin detection.
/// </summary>
public class JoinDetectionHelperTests
{
    #region Test Models

    private record Person(int Id, string Name, int Age);
    private record Order(int Id, int PersonId, decimal Amount);

    #endregion

    [Fact]
    public void IsGroupJoin_WithGroupJoinSelector_ReturnsTrue()
    {
        // Arrange - GroupJoin: Func<Person, IEnumerable<Order>, TResult>
        Func<Person, IEnumerable<Order>, string> groupJoinSelector = 
            (p, orders) => $"{p.Name}: {orders.Count()} orders";
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(groupJoinSelector);
        
        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsGroupJoin_WithJoinSelector_ReturnsFalse()
    {
        // Arrange - Join: Func<Person, Order, TResult>
        Func<Person, Order, string> joinSelector = 
            (p, o) => $"{p.Name}: Order {o.Id}";
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(joinSelector);
        
        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsGroupJoin_WithGroupJoinSelectorAnonymousType_ReturnsTrue()
    {
        // Arrange - GroupJoin with anonymous result type
        Func<Person, IEnumerable<Order>, object> groupJoinSelector = 
            (p, orders) => new { p.Name, OrderCount = orders.Count() };
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(groupJoinSelector);
        
        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsGroupJoin_WithJoinSelectorAnonymousType_ReturnsFalse()
    {
        // Arrange - Join with anonymous result type
        Func<Person, Order, object> joinSelector = 
            (p, o) => new { p.Name, OrderId = o.Id };
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(joinSelector);
        
        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsGroupJoin_WithCompiledLambda_HandlesClosureParameter()
    {
        // Arrange - Compiled lambda may have Closure parameter
        // Simulate by creating a delegate from a lambda expression
        var localVar = "test";
        Func<Person, IEnumerable<Order>, string> groupJoinWithClosure = 
            (p, orders) => $"{localVar}: {p.Name} - {orders.Count()}";
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(groupJoinWithClosure);
        
        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsGroupJoin_WithNullSelector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => 
            JoinDetectionHelper.IsGroupJoin(null!));
    }

    [Fact]
    public void IsGroupJoin_WithSingleParameterSelector_ThrowsInvalidOperationException()
    {
        // Arrange - Invalid: only one parameter (not a valid join selector)
        Func<Person, string> invalidSelector = p => p.Name;
        
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => 
            JoinDetectionHelper.IsGroupJoin(invalidSelector));
        
        exception.Message.ShouldContain("at least 2 parameters");
    }

    [Fact]
    public void IsGroupJoin_WithDifferentIEnumerableTypes_ReturnsTrue()
    {
        // Arrange - GroupJoin with IEnumerable<Order> (actual interface type)
        Func<Person, IEnumerable<Order>, string> groupJoinSelector = 
            (p, orders) => $"{p.Name}: {orders.Count()} orders";
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(groupJoinSelector);
        
        // Assert
        result.ShouldBeTrue();
    }
    
    [Fact]
    public void IsGroupJoin_WithListParameter_ReturnsFalse()
    {
        // Arrange - When parameter is List<Order>, not IEnumerable<Order>
        // List<> has different generic type definition than IEnumerable<>
        Func<Person, List<Order>, string> listSelector = 
            (p, orders) => $"{p.Name}: {orders.Count} orders";
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(listSelector);
        
        // Assert
        // List<T> generic type definition != IEnumerable<T> generic type definition
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsGroupJoin_WithIQueryable_ReturnsFalse()
    {
        // Arrange - Second parameter is IQueryable<Order>, not IEnumerable<Order>
        // This should be treated as Join, not GroupJoin
        Func<Person, IQueryable<Order>, string> queryableSelector = 
            (p, orders) => $"{p.Name}";
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(queryableSelector);
        
        // Assert
        // IQueryable<T> is not IEnumerable<T> in terms of the generic type definition check
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsGroupJoin_WithComplexGenericTypes_DetectsCorrectly()
    {
        // Arrange - GroupJoin with nested generic types
        Func<Person, IEnumerable<Order>, Dictionary<int, List<string>>> complexGroupJoin = 
            (p, orders) => orders.GroupBy(o => o.PersonId)
                                .ToDictionary(g => g.Key, g => g.Select(o => o.Id.ToString()).ToList());
        
        // Act
        var result = JoinDetectionHelper.IsGroupJoin(complexGroupJoin);
        
        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsGroupJoin_ConsistentResultsOnMultipleCalls()
    {
        // Arrange
        Func<Person, IEnumerable<Order>, string> groupJoinSelector = 
            (p, orders) => $"{p.Name}: {orders.Count()}";
        
        // Act - Call multiple times
        var result1 = JoinDetectionHelper.IsGroupJoin(groupJoinSelector);
        var result2 = JoinDetectionHelper.IsGroupJoin(groupJoinSelector);
        var result3 = JoinDetectionHelper.IsGroupJoin(groupJoinSelector);
        
        // Assert - Should always return true
        result1.ShouldBeTrue();
        result2.ShouldBeTrue();
        result3.ShouldBeTrue();
    }
}
