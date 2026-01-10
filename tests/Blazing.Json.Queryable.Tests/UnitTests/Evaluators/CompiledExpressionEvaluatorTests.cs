using Blazing.Json.Queryable.Implementations;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Evaluators;

/// <summary>
/// Unit tests for CompiledExpressionEvaluator.
/// </summary>
public class CompiledExpressionEvaluatorTests
{
    private readonly CompiledExpressionEvaluator _evaluator;

    public CompiledExpressionEvaluatorTests()
    {
        _evaluator = new CompiledExpressionEvaluator();
    }

    [Fact]
    public void BuildPredicate_WithSimpleExpression_ReturnsWorkingPredicate()
    {
        // Arrange
        var testPerson = new Person { Name = "Alice", Age = 25 };

        // Act
        var predicate = _evaluator.BuildPredicate<Person>(p => p.Age > 20);

        // Assert
        predicate.ShouldNotBeNull();
        predicate(testPerson).ShouldBeTrue();
    }

    [Fact]
    public void BuildPredicate_WithComplexExpression_ReturnsWorkingPredicate()
    {
        // Arrange
        var testPerson = new Person { Name = "Bob", Age = 30, City = "New York", IsActive = true };

        // Act
        var predicate = _evaluator.BuildPredicate<Person>(p => p.Age > 25 && p.City == "New York" && p.IsActive);

        // Assert
        predicate.ShouldNotBeNull();
        predicate(testPerson).ShouldBeTrue();
    }

    [Fact]
    public void BuildPredicate_CachesCompiledDelegate()
    {
        // Arrange
        System.Linq.Expressions.Expression<Func<Person, bool>> expression = p => p.Age > 20;

        // Act
        var predicate1 = _evaluator.BuildPredicate(expression);
        var initialCacheCount = _evaluator.PredicateCacheCount;
        var predicate2 = _evaluator.BuildPredicate(expression);
        var finalCacheCount = _evaluator.PredicateCacheCount;

        // Assert
        initialCacheCount.ShouldBe(1);
        finalCacheCount.ShouldBe(1); // Should not increase
        predicate1.ShouldBeSameAs(predicate2); // Same instance from cache
    }

    [Fact]
    public void BuildPredicate_WithNullExpression_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _evaluator.BuildPredicate<Person>(null!));
    }

    [Fact]
    public void BuildSelector_WithSimpleProjection_ReturnsWorkingSelector()
    {
        // Arrange
        var testPerson = new Person { Name = "Alice", Age = 25 };

        // Act
        var selector = _evaluator.BuildSelector<Person, string>(p => p.Name);

        // Assert
        selector.ShouldNotBeNull();
        selector(testPerson).ShouldBe("Alice");
    }

    [Fact]
    public void BuildSelector_WithAnonymousTypeProjection_ReturnsWorkingSelector()
    {
        // Arrange
        var testPerson = new Person { Name = "Bob", Age = 30 };

        // Act
        var selector = _evaluator.BuildSelector<Person, PersonDto>(p => new PersonDto { Name = p.Name, Age = p.Age });

        // Assert
        selector.ShouldNotBeNull();
        var result = selector(testPerson);
        result.Name.ShouldBe("Bob");
        result.Age.ShouldBe(30);
    }

    [Fact]
    public void BuildSelector_CachesCompiledDelegate()
    {
        // Arrange
        System.Linq.Expressions.Expression<Func<Person, string>> expression = p => p.Name;

        // Act
        var selector1 = _evaluator.BuildSelector(expression);
        var initialCacheCount = _evaluator.SelectorCacheCount;
        var selector2 = _evaluator.BuildSelector(expression);
        var finalCacheCount = _evaluator.SelectorCacheCount;

        // Assert
        initialCacheCount.ShouldBe(1);
        finalCacheCount.ShouldBe(1); // Should not increase
        selector1.ShouldBeSameAs(selector2); // Same instance from cache
    }

    [Fact]
    public void BuildSelector_WithNullExpression_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _evaluator.BuildSelector<Person, string>(null!));
    }

    [Fact]
    public void ClearCache_RemovesAllCachedDelegates()
    {
        // Arrange
        _evaluator.BuildPredicate<Person>(p => p.Age > 20);
        _evaluator.BuildSelector<Person, string>(p => p.Name);

        // Act
        _evaluator.ClearCache();

        // Assert
        _evaluator.PredicateCacheCount.ShouldBe(0);
        _evaluator.SelectorCacheCount.ShouldBe(0);
    }

    [Fact]
    public void BuildPredicate_WithMultipleConditions_WorksCorrectly()
    {
        // Arrange
        var people = TestData.GetSmallPersonDataset();

        // Act
        var predicate = _evaluator.BuildPredicate<Person>(p => p.Age >= 25 && p.Age <= 35 && p.IsActive);
        var filtered = people.Where(predicate).ToList();

        // Assert
        filtered.ShouldNotBeEmpty();
        filtered.ShouldAllBe(p => p.Age >= 25 && p.Age <= 35 && p.IsActive);
    }

    [Fact]
    public void BuildSelector_WithComputedProperty_WorksCorrectly()
    {
        // Arrange
        var testPerson = new Person { Name = "Alice", Age = 25 };

        // Act
        var selector = _evaluator.BuildSelector<Person, PersonDto>(p => new PersonDto 
        { 
            Name = p.Name, 
            Age = p.Age 
        });

        // Assert
        var result = selector(testPerson);
        result.IsAdult.ShouldBeTrue(); // Computed property based on Age
    }
}
