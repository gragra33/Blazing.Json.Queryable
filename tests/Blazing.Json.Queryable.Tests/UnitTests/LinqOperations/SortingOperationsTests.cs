using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for sorting operations: OrderBy, OrderByDescending, ThenBy, ThenByDescending, Reverse.
/// </summary>
public class SortingOperationsTests
{
    #region OrderBy and ThenBy Tests

    [Fact]
    public void OrderBy_SingleLevel_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 3, Name = "Charlie", Age = 35 },
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Age)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Age.ShouldBe(25);
        results[1].Age.ShouldBe(30);
        results[2].Age.ShouldBe(35);
    }

    [Fact]
    public void OrderByDescending_SingleLevel_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 3, Name = "Charlie", Age = 35 },
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderByDescending(p => p.Age)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Age.ShouldBe(35);
        results[1].Age.ShouldBe(30);
        results[2].Age.ShouldBe(25);
    }

    [Fact]
    public void OrderBy_ThenBy_TwoLevels_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Charlie", Age = 30, City = "London" },
            new() { Id = 2, Name = "Alice", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Bob", Age = 25, City = "London" },
            new() { Id = 4, Name = "David", Age = 30, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Age)
            .ThenBy(p => p.Name)
            .ToList();

        // Assert
        results.Count.ShouldBe(4);
        // First by Age (25, then 30s)
        results[0].Age.ShouldBe(25);
        results[0].Name.ShouldBe("Bob");
        // Then by Name within Age=30
        results[1].Age.ShouldBe(30);
        results[1].Name.ShouldBe("Alice");
        results[2].Age.ShouldBe(30);
        results[2].Name.ShouldBe("Charlie");
        results[3].Age.ShouldBe(30);
        results[3].Name.ShouldBe("David");
    }

    [Fact]
    public void OrderBy_ThenByDescending_TwoLevels_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Charlie", Age = 30, City = "London" },
            new() { Id = 2, Name = "Alice", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Bob", Age = 25, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Age)
            .ThenByDescending(p => p.Name)
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Age.ShouldBe(25);
        results[0].Name.ShouldBe("Bob");
        results[1].Age.ShouldBe(30);
        results[1].Name.ShouldBe("Charlie"); // Descending by name
        results[2].Age.ShouldBe(30);
        results[2].Name.ShouldBe("Alice");
    }

    [Fact]
    public void OrderBy_ThenBy_ThenBy_ThreeLevels_SortsCorrectly()
    {
        // Arrange
        var data = new List<Product>
        {
            new() { Id = 1, Name = "Widget", Category = "Tools", Rating = 4.5, Price = 25.00m },
            new() { Id = 2, Name = "Gadget", Category = "Tools", Rating = 4.5, Price = 15.00m },
            new() { Id = 3, Name = "Device", Category = "Electronics", Rating = 4.8, Price = 50.00m },
            new() { Id = 4, Name = "Doohickey", Category = "Tools", Rating = 4.5, Price = 20.00m },
            new() { Id = 5, Name = "Gizmo", Category = "Tools", Rating = 4.2, Price = 30.00m }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Product>.FromString(json)
            .OrderBy(p => p.Category)      // 1st level: Category
            .ThenBy(p => p.Rating)          // 2nd level: Rating
            .ThenBy(p => p.Price)           // 3rd level: Price
            .ToList();

        // Assert
        results.Count.ShouldBe(5);
        
        // Electronics first (only one)
        results[0].Category.ShouldBe("Electronics");
        results[0].Name.ShouldBe("Device");
        
        // Tools, sorted by Rating, then by Price
        results[1].Category.ShouldBe("Tools");
        results[1].Rating.ShouldBe(4.2);
        results[1].Price.ShouldBe(30.00m);
        
        results[2].Category.ShouldBe("Tools");
        results[2].Rating.ShouldBe(4.5);
        results[2].Price.ShouldBe(15.00m); // Lowest price among 4.5 ratings
        results[2].Name.ShouldBe("Gadget");
        
        results[3].Category.ShouldBe("Tools");
        results[3].Rating.ShouldBe(4.5);
        results[3].Price.ShouldBe(20.00m); // Middle price
        results[3].Name.ShouldBe("Doohickey");
        
        results[4].Category.ShouldBe("Tools");
        results[4].Rating.ShouldBe(4.5);
        results[4].Price.ShouldBe(25.00m); // Highest price among 4.5 ratings
        results[4].Name.ShouldBe("Widget");
    }

    [Fact]
    public void OrderBy_ThenByDescending_ThenBy_ThreeLevels_SortsCorrectly()
    {
        // Arrange - This matches the sample scenario
        var data = new List<Product>
        {
            new() { Id = 1, Name = "Mechanical Keyboard", Category = "Accessories", Rating = 4.7, Price = 89.99m },
            new() { Id = 2, Name = "Docking Station", Category = "Accessories", Rating = 4.4, Price = 249.99m },
            new() { Id = 3, Name = "Mouse Pad XL", Category = "Accessories", Rating = 4.3, Price = 14.99m },
            new() { Id = 4, Name = "Wireless Mouse", Category = "Accessories", Rating = 4.2, Price = 29.99m },
            new() { Id = 5, Name = "HD Webcam", Category = "Accessories", Rating = 4.1, Price = 79.99m },
            new() { Id = 6, Name = "Gaming Headset", Category = "Audio", Rating = 4.8, Price = 149.99m },
            new() { Id = 7, Name = "USB-C Cable", Category = "Cables", Rating = 4.0, Price = 9.99m },
            new() { Id = 8, Name = "Laptop Air", Category = "Computers", Rating = 4.9, Price = 1499.99m },
            new() { Id = 9, Name = "Laptop Pro", Category = "Computers", Rating = 4.5, Price = 1299.99m },
            new() { Id = 10, Name = "4K Monitor", Category = "Displays", Rating = 4.6, Price = 349.99m }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Product>.FromString(json)
            .OrderBy(p => p.Category)           // 1st: Category (ascending)
            .ThenByDescending(p => p.Rating)    // 2nd: Rating (descending)
            .ThenBy(p => p.Price)               // 3rd: Price (ascending)
            .ToList();

        // Assert
        results.Count.ShouldBe(10);
        
        // Accessories (sorted by Rating desc, then Price asc)
        results[0].Category.ShouldBe("Accessories");
        results[0].Rating.ShouldBe(4.7);
        results[0].Price.ShouldBe(89.99m);
        results[0].Name.ShouldBe("Mechanical Keyboard");
        
        results[1].Category.ShouldBe("Accessories");
        results[1].Rating.ShouldBe(4.4);
        results[1].Price.ShouldBe(249.99m);
        results[1].Name.ShouldBe("Docking Station");
        
        results[2].Category.ShouldBe("Accessories");
        results[2].Rating.ShouldBe(4.3);
        results[2].Price.ShouldBe(14.99m);
        results[2].Name.ShouldBe("Mouse Pad XL");
        
        results[3].Category.ShouldBe("Accessories");
        results[3].Rating.ShouldBe(4.2);
        results[3].Price.ShouldBe(29.99m);
        results[3].Name.ShouldBe("Wireless Mouse");
        
        results[4].Category.ShouldBe("Accessories");
        results[4].Rating.ShouldBe(4.1);
        results[4].Price.ShouldBe(79.99m);
        results[4].Name.ShouldBe("HD Webcam");
        
        // Audio
        results[5].Category.ShouldBe("Audio");
        results[5].Name.ShouldBe("Gaming Headset");
        
        // Cables
        results[6].Category.ShouldBe("Cables");
        results[6].Name.ShouldBe("USB-C Cable");
        
        // Computers (sorted by Rating desc, then Price asc)
        results[7].Category.ShouldBe("Computers");
        results[7].Rating.ShouldBe(4.9);
        results[7].Price.ShouldBe(1499.99m);
        results[7].Name.ShouldBe("Laptop Air");
        
        results[8].Category.ShouldBe("Computers");
        results[8].Rating.ShouldBe(4.5);
        results[8].Price.ShouldBe(1299.99m);
        results[8].Name.ShouldBe("Laptop Pro");
        
        // Displays
        results[9].Category.ShouldBe("Displays");
        results[9].Name.ShouldBe("4K Monitor");
    }

    [Fact]
    public void OrderBy_ThenBy_ThenBy_ThenBy_FourLevels_SortsCorrectly()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 30, City = "London", Department = "Engineering" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "London", Department = "Engineering" },
            new() { Id = 3, Name = "Charlie", Age = 30, City = "London", Department = "Sales" },
            new() { Id = 4, Name = "David", Age = 30, City = "Paris", Department = "Engineering" },
            new() { Id = 5, Name = "Eve", Age = 25, City = "London", Department = "Engineering" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Age)            // 1st: Age
            .ThenBy(p => p.City)            // 2nd: City
            .ThenBy(p => p.Department)      // 3rd: Department
            .ThenBy(p => p.Name)            // 4th: Name
            .ToList();

        // Assert
        results.Count.ShouldBe(5);
        
        // Age 25 first
        results[0].Age.ShouldBe(25);
        results[0].Name.ShouldBe("Eve");
        
        // Age 30, London, Engineering (Alice before Bob)
        results[1].Age.ShouldBe(30);
        results[1].City.ShouldBe("London");
        results[1].Department.ShouldBe("Engineering");
        results[1].Name.ShouldBe("Alice");
        
        results[2].Age.ShouldBe(30);
        results[2].City.ShouldBe("London");
        results[2].Department.ShouldBe("Engineering");
        results[2].Name.ShouldBe("Bob");
        
        // Age 30, London, Sales
        results[3].Age.ShouldBe(30);
        results[3].City.ShouldBe("London");
        results[3].Department.ShouldBe("Sales");
        results[3].Name.ShouldBe("Charlie");
        
        // Age 30, Paris
        results[4].Age.ShouldBe(30);
        results[4].City.ShouldBe("Paris");
        results[4].Name.ShouldBe("David");
    }

    [Fact]
    public void OrderByDescending_ThenByDescending_ThenByDescending_ThreeLevels_SortsCorrectly()
    {
        // Arrange
        var data = new List<Product>
        {
            new() { Id = 1, Name = "A", Category = "Tools", Rating = 4.5, Price = 25.00m },
            new() { Id = 2, Name = "B", Category = "Tools", Rating = 4.5, Price = 15.00m },
            new() { Id = 3, Name = "C", Category = "Electronics", Rating = 4.8, Price = 50.00m },
            new() { Id = 4, Name = "D", Category = "Tools", Rating = 4.2, Price = 30.00m }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Product>.FromString(json)
            .OrderByDescending(p => p.Category)
            .ThenByDescending(p => p.Rating)
            .ThenByDescending(p => p.Price)
            .ToList();

        // Assert
        results.Count.ShouldBe(4);
        
        // Tools first (descending), Rating descending, Price descending
        results[0].Category.ShouldBe("Tools");
        results[0].Rating.ShouldBe(4.5);
        results[0].Price.ShouldBe(25.00m);
        
        results[1].Category.ShouldBe("Tools");
        results[1].Rating.ShouldBe(4.5);
        results[1].Price.ShouldBe(15.00m);
        
        results[2].Category.ShouldBe("Tools");
        results[2].Rating.ShouldBe(4.2);
        results[2].Price.ShouldBe(30.00m);
        
        // Electronics last
        results[3].Category.ShouldBe("Electronics");
    }

    #endregion

    #region Reverse Tests

    [Fact]
    public void Reverse_ReversesSequenceOrder()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(5);
        results.ShouldBe([5, 4, 3, 2, 1]);
    }

    [Fact]
    public void Reverse_WithOrderBy_ReversesAfterSorting()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 3, Name = "Charlie", Age = 35 },
            new() { Id = 1, Name = "Alice", Age = 25 },
            new() { Id = 2, Name = "Bob", Age = 30 }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .OrderBy(p => p.Age)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results[0].Age.ShouldBe(35);
        results[1].Age.ShouldBe(30);
        results[2].Age.ShouldBe(25);
    }

    [Fact]
    public void Reverse_EmptySequence_ReturnsEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Reverse()
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Reverse_SingleElement_ReturnsSameElement()
    {
        // Arrange
        var data = new List<string> { "Alice" };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<string>.FromString(json)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe("Alice");
    }

    [Fact]
    public void Reverse_WithFilter_FiltersFirstThenReverses()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5, 6 };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<int>.FromString(json)
            .Where(n => n % 2 == 0)
            .Reverse()
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe([6, 4, 2]);
    }

    #endregion
}
