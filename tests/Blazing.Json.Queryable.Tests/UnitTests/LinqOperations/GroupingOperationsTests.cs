using Blazing.Json.Queryable.Providers;
using Blazing.Json.Queryable.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace Blazing.Json.Queryable.Tests.UnitTests.LinqOperations;

/// <summary>
/// Tests for grouping operations: GroupBy, ToLookup.
/// </summary>
public class GroupingOperationsTests
{
    [Fact]
    public void GroupBy_GroupsByKey()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" },
            new() { Id = 4, Name = "David", Age = 40, City = "Paris" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        var londonGroup = results.First(g => g.Key == "London");
        londonGroup.Count().ShouldBe(2);
        var parisGroup = results.First(g => g.Key == "Paris");
        parisGroup.Count().ShouldBe(2);
    }

    [Fact]
    public void GroupBy_WithElementSelector_ProjectsElements()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => p.Name)
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        var londonGroup = results.First(g => g.Key == "London");
        londonGroup.ShouldContain("Alice");
        londonGroup.ShouldContain("Charlie");
    }

    [Fact]
    public void GroupBy_WithResultSelector_ProjectsGroups()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(
                p => p.City,
                (city, people) => new { City = city, Count = people.Count() })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.First(r => r.City == "London").Count.ShouldBe(2);
        results.First(r => r.City == "Paris").Count.ShouldBe(1);
    }

    [Fact]
    public void GroupBy_WithElementAndResultSelector_ProjectsBoth()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(
                p => p.City,
                p => p.Age,
                (city, ages) => new { City = city, AverageAge = ages.Average() })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.First(r => r.City == "London").AverageAge.ShouldBe(30.0);
        results.First(r => r.City == "Paris").AverageAge.ShouldBe(30.0);
    }

    [Fact]
    public void ToLookup_CreatesLookup()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var lookup = JsonQueryable<Person>.FromString(json)
            .ToLookup(p => p.City);

        // Assert
        lookup.Count.ShouldBe(2);
        lookup["London"].Count().ShouldBe(2);
        lookup["Paris"].Count().ShouldBe(1);
        lookup["Unknown"].Count().ShouldBe(0);
    }

    [Fact]
    public void ToLookup_WithElementSelector_ProjectsElements()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Paris" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var lookup = JsonQueryable<Person>.FromString(json)
            .ToLookup(p => p.City, p => p.Name);

        // Assert
        lookup["London"].ShouldContain("Alice");
        lookup["London"].ShouldContain("Charlie");
        lookup["Paris"].ShouldContain("Bob");
    }

    [Fact]
    public void GroupBy_EmptySequence_ReturnsEmpty()
    {
        // Arrange
        var json = TestData.GetEmptyJsonArray();

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void GroupBy_SingleGroup_ReturnsOneGroup()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "London" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "London" }
        };
        var json = TestData.SerializeToJson(data);

        // Act
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Count().ShouldBe(2);
    }

    #region Complex GroupBy Scenarios - Element Selector with Select

    [Fact]
    public void GroupBy_WithElementSelector_ThenSelect_WithSingleAggregation()
    {
        // Arrange - This is the bug scenario that was fixed
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Score = 92 },
            new() { Id = 4, Name = "David", Age = 40, City = "Portland", Score = 85 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - GroupBy with element selector, then Select with aggregation
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => p.Score)
            .Select(g => new
            {
                City = g.Key,
                AverageScore = g.Average()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        var seattleResult = results.First(r => r.City == "Seattle");
        seattleResult.AverageScore.ShouldBe(91.5);
        var portlandResult = results.First(r => r.City == "Portland");
        portlandResult.AverageScore.ShouldBe(88.5);
    }

    [Fact]
    public void GroupBy_WithElementSelector_ThenSelect_WithMultipleAggregations()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Score = 92 },
            new() { Id = 4, Name = "David", Age = 40, City = "Portland", Score = 85 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Multiple aggregations on grouped elements
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => p.Score)
            .Select(g => new
            {
                City = g.Key,
                AverageScore = g.Average(),
                HighScore = g.Max(),
                LowScore = g.Min(),
                Count = g.Count()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        var seattleResult = results.First(r => r.City == "Seattle");
        seattleResult.AverageScore.ShouldBe(91.5);
        seattleResult.HighScore.ShouldBe(95);
        seattleResult.LowScore.ShouldBe(88);
        seattleResult.Count.ShouldBe(2);
    }

    [Fact]
    public void GroupBy_WithElementSelector_ThenSelect_WithConditionalAggregation()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Score = 92 },
            new() { Id = 4, Name = "David", Age = 40, City = "Portland", Score = 85 },
            new() { Id = 5, Name = "Eve", Age = 28, City = "Seattle", Score = 78 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Aggregation with filtering inside Select
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => p.Score)
            .Select(g => new
            {
                City = g.Key,
                HighScoresAverage = g.Where(score => score >= 90).DefaultIfEmpty(0).Average(),
                AllScoresAverage = g.Average()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        var seattleResult = results.First(r => r.City == "Seattle");
        seattleResult.HighScoresAverage.ShouldBe(95.0); // Only Alice's score >= 90
        seattleResult.AllScoresAverage.ShouldBe((95.0 + 88.0 + 78.0) / 3);
    }

    [Fact]
    public void GroupBy_WithComplexElementSelector_ThenSelect()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Score = 92 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Element selector returns anonymous type
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => new { p.Name, p.Score })
            .Select(g => new
            {
                City = g.Key,
                TopScorer = g.OrderByDescending(x => x.Score).First().Name,
                AverageScore = g.Average(x => x.Score)
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        var seattleResult = results.First(r => r.City == "Seattle");
        seattleResult.TopScorer.ShouldBe("Alice");
        seattleResult.AverageScore.ShouldBe(91.5);
    }

    #endregion

    #region Nested Grouping Scenarios

    [Fact]
    public void GroupBy_Nested_TwoLevelGrouping()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Department = "Engineering" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Department = "Sales" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Department = "Engineering" },
            new() { Id = 4, Name = "David", Age = 40, City = "Portland", Department = "Sales" }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Group by City, then within each city group by Department
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                Departments = cityGroup
                    .GroupBy(p => p.Department)
                    .Select(deptGroup => new
                    {
                        Department = deptGroup.Key,
                        Count = deptGroup.Count()
                    })
                    .ToList()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        
        var seattleResult = results.First(r => r.City == "Seattle");
        seattleResult.Departments.Count.ShouldBe(2);
        seattleResult.Departments.First(d => d.Department == "Engineering").Count.ShouldBe(1);
        seattleResult.Departments.First(d => d.Department == "Sales").Count.ShouldBe(1);
    }

    [Fact]
    public void GroupBy_Nested_WithElementSelector()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Department = "Engineering", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Department = "Engineering", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Seattle", Department = "Sales", Score = 92 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Nested grouping with element selectors
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                DepartmentStats = cityGroup
                    .GroupBy(p => p.Department, p => p.Score)
                    .Select(deptGroup => new
                    {
                        Department = deptGroup.Key,
                        AverageScore = deptGroup.Average(),
                        MaxScore = deptGroup.Max()
                    })
                    .ToList()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(1);
        var seattleResult = results.First();
        seattleResult.City.ShouldBe("Seattle");
        
        var engStats = seattleResult.DepartmentStats.First(d => d.Department == "Engineering");
        engStats.AverageScore.ShouldBe(91.5);
        engStats.MaxScore.ShouldBe(95);
    }

    #endregion

    #region Regrouping Scenarios (GroupBy -> Select -> GroupBy)

    [Fact]
    public void GroupBy_ThenSelect_ThenGroupByAgain_SimpleRegroup()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Department = "Engineering" },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Department = "Sales" },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Department = "Engineering" },
            new() { Id = 4, Name = "David", Age = 40, City = "Portland", Department = "Sales" }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Group by City, project to new shape, then regroup by Department
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .SelectMany(cityGroup => cityGroup.Select(p => new
            {
                p.Department,
                p.Name,
                CityCount = cityGroup.Count()
            }))
            .GroupBy(x => x.Department)
            .Select(deptGroup => new
            {
                Department = deptGroup.Key,
                EmployeeCount = deptGroup.Count(),
                Names = deptGroup.Select(x => x.Name).ToList()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        
        var engineeringResult = results.First(r => r.Department == "Engineering");
        engineeringResult.EmployeeCount.ShouldBe(2);
        engineeringResult.Names.ShouldContain("Alice");
        engineeringResult.Names.ShouldContain("Charlie");
    }

    [Fact]
    public void GroupBy_ThenSelect_ThenGroupByAgain_WithAggregations()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Score = 92 },
            new() { Id = 4, Name = "David", Age = 40, City = "Portland", Score = 85 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Group by City with aggregation, then regroup by age range
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => p.Score)
            .Select(g => new
            {
                City = g.Key,
                AverageScore = g.Average(),
                MaxScore = g.Max()
            })
            .GroupBy(x => x.AverageScore >= 90 ? "High" : "Medium")
            .Select(g => new
            {
                ScoreCategory = g.Key,
                CityCount = g.Count(),
                Cities = g.Select(x => x.City).ToList()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        
        var highCategory = results.First(r => r.ScoreCategory == "High");
        highCategory.CityCount.ShouldBe(1);
        highCategory.Cities.ShouldContain("Seattle");
    }

    #endregion

    #region Deep Nesting Scenarios

    [Fact]
    public void GroupBy_ThreeLevelNesting_CityDepartmentAgeRange()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Department = "Engineering", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 35, City = "Seattle", Department = "Engineering", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 25, City = "Seattle", Department = "Sales", Score = 92 },
            new() { Id = 4, Name = "David", Age = 35, City = "Portland", Department = "Engineering", Score = 85 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Three levels: City -> Department -> Age Range
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City)
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                Departments = cityGroup
                    .GroupBy(p => p.Department)
                    .Select(deptGroup => new
                    {
                        Department = deptGroup.Key,
                        AgeRanges = deptGroup
                            .GroupBy(p => p.Age < 30 ? "Under30" : "Over30")
                            .Select(ageGroup => new
                            {
                                AgeRange = ageGroup.Key,
                                Count = ageGroup.Count(),
                                AverageScore = ageGroup.Average(p => p.Score)
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        
        var seattleResult = results.First(r => r.City == "Seattle");
        var seattleEng = seattleResult.Departments.First(d => d.Department == "Engineering");
        seattleEng.AgeRanges.Count.ShouldBe(2); // Both age ranges represented
    }

    [Fact]
    public void GroupBy_ComplexPipeline_MultipleRegroupingsWithElementSelectors()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Department = "Engineering", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Department = "Engineering", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Department = "Sales", Score = 92 },
            new() { Id = 4, Name = "David", Age = 40, City = "Portland", Department = "Sales", Score = 85 },
            new() { Id = 5, Name = "Eve", Age = 28, City = "Seattle", Department = "Sales", Score = 90 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Complex pipeline: Group -> Select with element selector -> Regroup -> Aggregate
        var results = JsonQueryable<Person>.FromString(json)
            // First grouping by City with element selector
            .GroupBy(p => p.City, p => new { p.Department, p.Score, p.Age })
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                DepartmentScores = cityGroup
                    // Second grouping by Department
                    .GroupBy(x => x.Department, x => x.Score)
                    .Select(deptGroup => new
                    {
                        Department = deptGroup.Key,
                        AverageScore = deptGroup.Average(),
                        Count = deptGroup.Count()
                    })
                    .ToList(),
                TotalEmployees = cityGroup.Count()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2);
        
        var seattleResult = results.First(r => r.City == "Seattle");
        seattleResult.TotalEmployees.ShouldBe(3);
        seattleResult.DepartmentScores.Count.ShouldBe(2);
        
        var seattleEng = seattleResult.DepartmentScores.First(d => d.Department == "Engineering");
        seattleEng.AverageScore.ShouldBe(91.5); // (95 + 88) / 2
        seattleEng.Count.ShouldBe(2);
    }

    [Fact]
    public void GroupBy_WithElementSelector_ThenOrderBy_ThenSelect()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Portland", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Boston", Score = 92 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - GroupBy with element selector, then order the groups, then select
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => p.Score)
            .OrderByDescending(g => g.Average())
            .Select(g => new
            {
                City = g.Key,
                AverageScore = g.Average()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(3);
        // Should be ordered by average score descending
        results[0].City.ShouldBe("Seattle"); // 95
        results[1].City.ShouldBe("Boston");  // 92
        results[2].City.ShouldBe("Portland"); // 88
    }

    [Fact]
    public void GroupBy_WithElementSelector_ThenWhere_ThenSelect()
    {
        // Arrange
        var data = new List<Person>
        {
            new() { Id = 1, Name = "Alice", Age = 25, City = "Seattle", Score = 95 },
            new() { Id = 2, Name = "Bob", Age = 30, City = "Seattle", Score = 88 },
            new() { Id = 3, Name = "Charlie", Age = 35, City = "Portland", Score = 92 },
            new() { Id = 4, Name = "David", Age = 40, City = "Boston", Score = 70 }
        };
        var json = TestData.SerializeToJson(data);

        // Act - Filter groups after grouping with element selector
        var results = JsonQueryable<Person>.FromString(json)
            .GroupBy(p => p.City, p => p.Score)
            .Where(g => g.Average() >= 85)
            .Select(g => new
            {
                City = g.Key,
                AverageScore = g.Average(),
                Count = g.Count()
            })
            .ToList();

        // Assert
        results.Count.ShouldBe(2); // Boston filtered out (average 70)
        results.ShouldAllBe(r => r.AverageScore >= 85);
        results.ShouldContain(r => r.City == "Seattle");
        results.ShouldContain(r => r.City == "Portland");
    }

    #endregion
}
