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
    public SpanPropertyAccessorTests()
    {
        // Clear cache before each test for isolation
        SpanPropertyAccessor.ClearCache();
    }

    [Fact]
    public void GetValue_WithSpan_ReturnsCorrectValue()
    {
        // Arrange
        var person = new Person { Name = "Alice", Age = 25 };

        // Act
        var nameValue = SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var ageValue = SpanPropertyAccessor.GetValue(person, "Age".AsSpan());

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
        var value = SpanPropertyAccessor.GetValue(person, "NonExistent".AsSpan());

        // Assert
        value.ShouldBeNull();
    }

    [Fact]
    public void GetValue_CachesPropertyInfo()
    {
        // Arrange
        var person = new Person { Name = "Charlie" };

        // Act
        SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var initialCacheCount = SpanPropertyAccessor.CacheCount;
        SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var finalCacheCount = SpanPropertyAccessor.CacheCount;

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
        var value = SpanPropertyAccessor.GetValue(person, "name".AsSpan()); // lowercase

        // Assert
        value.ShouldBe("Diana");
    }

    [Fact]
    public void GetValueByName_WithString_WorksCorrectly()
    {
        // Arrange
        var person = new Person { Name = "Eve", City = "Boston" };

        // Act
        var nameValue = SpanPropertyAccessor.GetValueByName(person, "Name");
        var cityValue = SpanPropertyAccessor.GetValueByName(person, "City");

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
        Should.Throw<ArgumentNullException>(() => SpanPropertyAccessor.GetValueByName(person, null!));
    }

    [Fact]
    public void GetValue_WithNullObject_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => SpanPropertyAccessor.GetValue(null!, "Name".AsSpan()));
    }

    [Fact]
    public void GetPropertyType_WithValidProperty_ReturnsCorrectType()
    {
        // Act
        var nameType = SpanPropertyAccessor.GetPropertyType(typeof(Person), "Name".AsSpan());
        var ageType = SpanPropertyAccessor.GetPropertyType(typeof(Person), "Age".AsSpan());

        // Assert
        nameType.ShouldBe(typeof(string));
        ageType.ShouldBe(typeof(int));
    }

    [Fact]
    public void GetPropertyType_WithNonExistentProperty_ThrowsPropertyAccessException()
    {
        // Act & Assert
        var exception = Should.Throw<PropertyAccessException>(() => 
            SpanPropertyAccessor.GetPropertyType(typeof(Person), "NonExistent".AsSpan()));
        
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
        
        // Record initial cache count
        var initialCacheCount = SpanPropertyAccessor.CacheCount;
        
        // Add a property to the cache
        SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var cacheAfterGet = SpanPropertyAccessor.CacheCount;

        // Act
        SpanPropertyAccessor.ClearCache();

        // Assert
        // Cache should have grown by at least 1 after GetValue
        cacheAfterGet.ShouldBeGreaterThan(initialCacheCount);
        // Cache should be empty after clear
        SpanPropertyAccessor.CacheCount.ShouldBe(0);
    }

    [Fact]
    public void GetValue_WithNullableProperty_ReturnsCorrectValue()
    {
        // Arrange
        var person = new Person { City = "Seattle" };

        // Act
        var value = SpanPropertyAccessor.GetValue(person, "City".AsSpan());

        // Assert
        value.ShouldBe("Seattle");
    }

    [Fact]
    public void GetValue_WithNullablePropertyNull_ReturnsNull()
    {
        // Arrange
        var person = new Person { City = null };

        // Act
        var value = SpanPropertyAccessor.GetValue(person, "City".AsSpan());

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
        SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        SpanPropertyAccessor.GetValue(person, "Age".AsSpan());
        
        var initialCacheCount = SpanPropertyAccessor.CacheCount;

        // Act - Call multiple times with span-based access
        for (int i = 0; i < 100; i++)
        {
            SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
            SpanPropertyAccessor.GetValue(person, "Age".AsSpan());
        }

        // Assert - Cache should remain stable (no new allocations for property lookups)
        SpanPropertyAccessor.CacheCount.ShouldBe(initialCacheCount);
    }

    [Fact]
    public void GetValue_SpanVsString_BothUseCache()
    {
        // Arrange
        var person = new Person { Name = "Test" };

        // Act - Access via span
        var spanValue = SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var spanCacheCount = SpanPropertyAccessor.CacheCount;

        // Access via string method (should use same cache)
        var stringValue = SpanPropertyAccessor.GetValueByName(person, "Name");
        var stringCacheCount = SpanPropertyAccessor.CacheCount;

        // Assert
        spanValue.ShouldBe("Test");
        stringValue.ShouldBe("Test");
        spanCacheCount.ShouldBe(stringCacheCount); // Same cache entry used
    }

    [Fact]
    public void GetPropertyType_WithSpan_CachesTypeInfo()
    {
        // Arrange & Act - First call
        var type1 = SpanPropertyAccessor.GetPropertyType(typeof(Person), "Name".AsSpan());
        var cacheCount1 = SpanPropertyAccessor.CacheCount;

        // Second call - should use cache
        var type2 = SpanPropertyAccessor.GetPropertyType(typeof(Person), "Name".AsSpan());
        var cacheCount2 = SpanPropertyAccessor.CacheCount;

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
        var addressValue = SpanPropertyAccessor.GetValue(person, "Address".AsSpan());

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

        // Record baseline BEFORE accessing any properties in this test
        var baselineCount = SpanPropertyAccessor.CacheCount;

        // Act - Access properties and measure cache growth
        SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var growthAfterName = SpanPropertyAccessor.CacheCount - baselineCount;
        
        SpanPropertyAccessor.GetValue(person, "Age".AsSpan());
        var growthAfterAge = SpanPropertyAccessor.CacheCount - baselineCount;
        
        SpanPropertyAccessor.GetValue(person, "City".AsSpan());
        var growthAfterCity = SpanPropertyAccessor.CacheCount - baselineCount;

        // Assert - Cache should grow by exactly 1 for each NEW unique property accessed
        // This is resilient to concurrent test execution because we measure relative growth
        growthAfterName.ShouldBe(1, "Cache should grow by 1 after accessing Name");
        growthAfterAge.ShouldBe(2, "Cache should grow by 2 after accessing Name and Age");
        growthAfterCity.ShouldBe(3, "Cache should grow by 3 after accessing Name, Age, and City");
    }

    [Fact]
    public void GetValue_EmptySpan_ThrowsArgumentException()
    {
        // Arrange
        var person = new Person();

        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            SpanPropertyAccessor.GetValue(person, []));
    }

    [Fact]
    public void GetPropertyType_WithNullableProperty_ReturnsNullableType()
    {
        // Act
        var cityType = SpanPropertyAccessor.GetPropertyType(typeof(Person), "City".AsSpan());

        // Assert
        cityType.ShouldBe(typeof(string)); // City is string? but reflection returns string
    }

    #endregion
}
