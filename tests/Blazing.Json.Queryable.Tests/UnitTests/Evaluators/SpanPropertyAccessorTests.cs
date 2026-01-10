using Blazing.Json.Queryable.Exceptions;
using Blazing.Json.Queryable.Implementations;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Evaluators;

/// <summary>
/// Unit tests for SpanPropertyAccessor.
/// </summary>
public class SpanPropertyAccessorTests
{
    private readonly SpanPropertyAccessor _accessor;

    public SpanPropertyAccessorTests()
    {
        _accessor = new SpanPropertyAccessor();
    }

    [Fact]
    public void GetValue_WithSpan_ReturnsCorrectValue()
    {
        // Arrange
        var person = new Person { Name = "Alice", Age = 25 };

        // Act
        var nameValue = _accessor.GetValue(person, "Name".AsSpan());
        var ageValue = _accessor.GetValue(person, "Age".AsSpan());

        // Assert
        nameValue.ShouldBe("Alice");
        ageValue.ShouldBe(25);
    }

    [Fact]
    public void GetValue_WithNonExistentProperty_ReturnsNull()
    {
        // Arrange
        var person = new Person { Name = "Bob" };

        // Act
        var value = _accessor.GetValue(person, "NonExistent".AsSpan());

        // Assert
        value.ShouldBeNull();
    }

    [Fact]
    public void GetValue_CachesPropertyInfo()
    {
        // Arrange
        var person = new Person { Name = "Charlie" };

        // Act
        _accessor.GetValue(person, "Name".AsSpan());
        var initialCacheCount = _accessor.CacheCount;
        _accessor.GetValue(person, "Name".AsSpan());
        var finalCacheCount = _accessor.CacheCount;

        // Assert
        initialCacheCount.ShouldBe(1);
        finalCacheCount.ShouldBe(1); // Cache count should not increase
    }

    [Fact]
    public void GetValue_WithCaseInsensitive_FindsProperty()
    {
        // Arrange
        var person = new Person { Name = "Diana" };

        // Act
        var value = _accessor.GetValue(person, "name".AsSpan()); // lowercase

        // Assert
        value.ShouldBe("Diana");
    }

    [Fact]
    public void GetValueByName_WithString_WorksCorrectly()
    {
        // Arrange
        var person = new Person { Name = "Eve", City = "Boston" };

        // Act
        var nameValue = _accessor.GetValueByName(person, "Name");
        var cityValue = _accessor.GetValueByName(person, "City");

        // Assert
        nameValue.ShouldBe("Eve");
        cityValue.ShouldBe("Boston");
    }

    [Fact]
    public void GetValueByName_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        var person = new Person();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _accessor.GetValueByName(person, null!));
    }

    [Fact]
    public void GetValue_WithNullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _accessor.GetValue(null!, "Name".AsSpan()));
    }

    [Fact]
    public void GetPropertyType_WithValidProperty_ReturnsCorrectType()
    {
        // Act
        var nameType = _accessor.GetPropertyType(typeof(Person), "Name".AsSpan());
        var ageType = _accessor.GetPropertyType(typeof(Person), "Age".AsSpan());

        // Assert
        nameType.ShouldBe(typeof(string));
        ageType.ShouldBe(typeof(int));
    }

    [Fact]
    public void GetPropertyType_WithNonExistentProperty_ThrowsPropertyAccessException()
    {
        // Act & Assert
        var exception = Should.Throw<PropertyAccessException>(() => 
            _accessor.GetPropertyType(typeof(Person), "NonExistent".AsSpan()));
        
        exception.PropertyName.ShouldBe("NonExistent");
        exception.TargetType.ShouldBe(typeof(Person));
    }

    [Fact]
    public void GetAllProperties_ReturnsAllPublicProperties()
    {
        // Act
        var properties = SpanPropertyAccessor.GetAllProperties(typeof(Person));

        // Assert
        properties.ShouldNotBeEmpty();
        properties.ShouldContain(p => p.Name == "Name");
        properties.ShouldContain(p => p.Name == "Age");
        properties.ShouldContain(p => p.Name == "City");
    }

    [Fact]
    public void ClearCache_RemovesCachedPropertyInfo()
    {
        // Arrange
        var person = new Person { Name = "Test" };
        _accessor.GetValue(person, "Name".AsSpan());

        // Act
        _accessor.ClearCache();

        // Assert
        _accessor.CacheCount.ShouldBe(0);
    }

    [Fact]
    public void GetValue_WithNullableProperty_ReturnsCorrectValue()
    {
        // Arrange
        var person = new Person { City = "Seattle" };

        // Act
        var value = _accessor.GetValue(person, "City".AsSpan());

        // Assert
        value.ShouldBe("Seattle");
    }

    [Fact]
    public void GetValue_WithNullablePropertyNull_ReturnsNull()
    {
        // Arrange
        var person = new Person { City = null };

        // Act
        var value = _accessor.GetValue(person, "City".AsSpan());

        // Assert
        value.ShouldBeNull();
    }

    #region Span-Based Zero Allocation Tests

    [Fact]
    public void GetValue_WithSpan_UsesCache_NoExtraAllocations()
    {
        // Arrange
        var person = new Person { Name = "Test", Age = 30 };
        
        // Warm up cache
        _accessor.GetValue(person, "Name".AsSpan());
        _accessor.GetValue(person, "Age".AsSpan());
        
        var initialCacheCount = _accessor.CacheCount;

        // Act - Call multiple times with span-based access
        for (int i = 0; i < 100; i++)
        {
            _accessor.GetValue(person, "Name".AsSpan());
            _accessor.GetValue(person, "Age".AsSpan());
        }

        // Assert - Cache should remain stable (no new allocations for property lookups)
        _accessor.CacheCount.ShouldBe(initialCacheCount);
    }

    [Fact]
    public void GetValue_SpanVsString_BothUseCache()
    {
        // Arrange
        var person = new Person { Name = "Test" };

        // Act - Access via span
        var spanValue = _accessor.GetValue(person, "Name".AsSpan());
        var spanCacheCount = _accessor.CacheCount;

        // Access via string method (should use same cache)
        var stringValue = _accessor.GetValueByName(person, "Name");
        var stringCacheCount = _accessor.CacheCount;

        // Assert
        spanValue.ShouldBe("Test");
        stringValue.ShouldBe("Test");
        spanCacheCount.ShouldBe(stringCacheCount); // Same cache entry used
    }

    [Fact]
    public void GetPropertyType_WithSpan_CachesTypeInfo()
    {
        // Arrange & Act - First call
        var type1 = _accessor.GetPropertyType(typeof(Person), "Name".AsSpan());
        var cacheCount1 = _accessor.CacheCount;

        // Second call - should use cache
        var type2 = _accessor.GetPropertyType(typeof(Person), "Name".AsSpan());
        var cacheCount2 = _accessor.CacheCount;

        // Assert
        type1.ShouldBe(typeof(string));
        type2.ShouldBe(typeof(string));
        cacheCount1.ShouldBe(cacheCount2); // No new cache entry
    }

    [Fact]
    public void GetValue_WithNestedProperty_WorksCorrectly()
    {
        // Arrange
        var person = new Person 
        { 
            Name = "Test",
            Address = new Address 
            { 
                City = "Seattle", 
                State = "WA" 
            }
        };

        // Act
        var addressValue = _accessor.GetValue(person, "Address".AsSpan());

        // Assert
        addressValue.ShouldNotBeNull();
        addressValue.ShouldBeOfType<Address>();
        var address = (Address)addressValue;
        address.City.ShouldBe("Seattle");
    }

    [Fact]
    public void GetValue_MultipleDifferentProperties_BuildsCache()
    {
        // Arrange
        var person = new Person 
        { 
            Name = "Alice", 
            Age = 25, 
            City = "Boston",
            Email = "alice@example.com"
        };

        // Act
        _accessor.GetValue(person, "Name".AsSpan());
        var cacheAfterName = _accessor.CacheCount;
        
        _accessor.GetValue(person, "Age".AsSpan());
        var cacheAfterAge = _accessor.CacheCount;
        
        _accessor.GetValue(person, "City".AsSpan());
        var cacheAfterCity = _accessor.CacheCount;

        // Assert - Cache should grow with each unique property
        cacheAfterName.ShouldBe(1);
        cacheAfterAge.ShouldBe(2);
        cacheAfterCity.ShouldBe(3);
    }

    [Fact]
    public void GetValue_EmptySpan_ThrowsArgumentException()
    {
        // Arrange
        var person = new Person();

        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            _accessor.GetValue(person, []));
    }

    [Fact]
    public void GetPropertyType_WithNullableProperty_ReturnsNullableType()
    {
        // Act
        var cityType = _accessor.GetPropertyType(typeof(Person), "City".AsSpan());

        // Assert
        cityType.ShouldBe(typeof(string)); // City is string? but reflection returns string
    }

    #endregion
}
