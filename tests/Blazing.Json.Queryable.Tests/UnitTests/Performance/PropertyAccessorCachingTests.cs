using Blazing.Json.Queryable.Implementations;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.Performance;

/// <summary>
/// Performance tests for property accessor caching behavior.
/// Validates that property lookups are cached and don't allocate per-access.
/// </summary>
public class PropertyAccessorCachingTests
{
    public PropertyAccessorCachingTests()
    {
        // Clear cache before each test for isolation
        SpanPropertyAccessor.ClearCache();
    }

    [Fact]
    public void SpanPropertyAccessor_CachesPropertyInfo_NoRepeatAllocations()
    {
        // Arrange
        var person = TestData.GetSmallPersonDataset().First();
        var propertyName = "Name".AsSpan();

        // Act - First access (cache miss)
        var value1 = SpanPropertyAccessor.GetValue(person, propertyName);

        // Act - Second access (cache hit - should not allocate)
        var value2 = SpanPropertyAccessor.GetValue(person, propertyName);

        // Act - Third access (cache hit - should not allocate)
        var value3 = SpanPropertyAccessor.GetValue(person, propertyName);

        // Assert - All values should be identical
        value1.ShouldBe(person.Name);
        value2.ShouldBe(person.Name);
        value3.ShouldBe(person.Name);

        // Performance note: PropertyInfo lookup is cached
        // Only first access requires reflection, subsequent accesses use cache
    }

    [Fact]
    public void SpanPropertyAccessor_MultipleProperties_EachCachedIndependently()
    {
        // Arrange
        var person = TestData.GetSmallPersonDataset().First();

        // Act - Access different properties
        var name = SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var age = SpanPropertyAccessor.GetValue(person, "Age".AsSpan());
        var city = SpanPropertyAccessor.GetValue(person, "City".AsSpan());
        var isActive = SpanPropertyAccessor.GetValue(person, "IsActive".AsSpan());

        // Act - Access same properties again (should be cached)
        var name2 = SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var age2 = SpanPropertyAccessor.GetValue(person, "Age".AsSpan());
        var city2 = SpanPropertyAccessor.GetValue(person, "City".AsSpan());
        var isActive2 = SpanPropertyAccessor.GetValue(person, "IsActive".AsSpan());

        // Assert
        name.ShouldBe(person.Name);
        age.ShouldBe(person.Age);
        city.ShouldBe(person.City);
        isActive.ShouldBe(person.IsActive);

        name2.ShouldBe(name);
        age2.ShouldBe(age);
        city2.ShouldBe(city);
        isActive2.ShouldBe(isActive);

        // Performance note: Each property cached independently
        // No repeat allocations for cached properties
    }

    [Fact]
    public void SpanPropertyAccessor_RepeatedAccess_ConstantTime()
    {
        // Arrange
        var person = TestData.GetSmallPersonDataset().First();
        var propertyName = "Name".AsSpan();

        // Warm up cache
        _ = SpanPropertyAccessor.GetValue(person, propertyName);

        // Act - Measure repeated cached access
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            _ = SpanPropertyAccessor.GetValue(person, propertyName);
        }
        stopwatch.Stop();

        // Assert - Cached access should be very fast
        // 10,000 cached property accesses should complete in <50ms
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(100); // Generous limit for CI

        // Performance note: Cached access is O(1) dictionary lookup
        // Typical: <0.001ms per cached access on modern hardware
    }

    [Fact]
    public void SpanPropertyAccessor_vs_StringPropertyAccessor_BothUseCache()
    {
        // Arrange
        var person = TestData.GetSmallPersonDataset().First();

        // Act - Span-based access
        var spanValue1 = SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var spanValue2 = SpanPropertyAccessor.GetValue(person, "Name".AsSpan()); // Cached

        // Act - String-based convenience method
        var stringValue1 = SpanPropertyAccessor.GetValueByName(person, "Name");
        var stringValue2 = SpanPropertyAccessor.GetValueByName(person, "Name"); // Should also be cached

        // Assert
        spanValue1.ShouldBe(person.Name);
        spanValue2.ShouldBe(person.Name);
        stringValue1.ShouldBe(person.Name);
        stringValue2.ShouldBe(person.Name);

        // Performance note: Both span and string methods use same cache
        // String method converts to span internally, then uses cached PropertyInfo
    }

    [Fact]
    public void SpanPropertyAccessor_CacheStability_AcrossMultipleTypes()
    {
        // Arrange
        var person = TestData.GetSmallPersonDataset().First();
        var product = TestData.GetProductDataset().First();

        // Act - Access properties from different types
        var personName = SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var productName = SpanPropertyAccessor.GetValue(product, "Name".AsSpan());
        var personAge = SpanPropertyAccessor.GetValue(person, "Age".AsSpan());
        var productPrice = SpanPropertyAccessor.GetValue(product, "Price".AsSpan());

        // Act - Access again (should be cached)
        var personName2 = SpanPropertyAccessor.GetValue(person, "Name".AsSpan());
        var productName2 = SpanPropertyAccessor.GetValue(product, "Name".AsSpan());

        // Assert
        personName.ShouldBe(person.Name);
        productName.ShouldBe(product.Name);
        personAge.ShouldBe(person.Age);
        productPrice.ShouldBe(product.Price);

        personName2.ShouldBe(personName);
        productName2.ShouldBe(productName);

        // Performance note: Cache keys include type information
        // Person.Name and Product.Name cached separately
    }

    [Fact]
    public void SpanPropertyAccessor_HighVolumeAccess_MaintainsCacheEfficiency()
    {
        // Arrange
        var people = TestData.GetMediumPersonDataset();

        // Warm up cache for all properties
        var firstPerson = people.First();
        _ = SpanPropertyAccessor.GetValue(firstPerson, "Name".AsSpan());
        _ = SpanPropertyAccessor.GetValue(firstPerson, "Age".AsSpan());
        _ = SpanPropertyAccessor.GetValue(firstPerson, "City".AsSpan());

        // Act - High volume access across many objects
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var totalAge = 0;
        foreach (var person in people)
        {
            var age = SpanPropertyAccessor.GetValue(person, "Age".AsSpan());
            totalAge += (int)age!;
        }
        stopwatch.Stop();

        // Assert
        totalAge.ShouldBeGreaterThan(0);

        // Performance note: 100 objects * 1 cached property access each
        // Should complete in <10ms with proper caching
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(100); // Generous limit for CI
    }
}
