namespace Blazing.Json.Queryable.Tests.Fixtures;

/// <summary>
/// Test model representing a person.
/// </summary>
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? City { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public Address? Address { get; set; }
    public string? Department { get; set; }
    public int Score { get; set; }
}

/// <summary>
/// Test model representing an address.
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string? Country { get; set; }
}

/// <summary>
/// Test model representing a product.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
    public bool InStock => Stock > 0;
    public DateTime? LastRestocked { get; set; }
}

/// <summary>
/// Test model representing an order.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>
/// Test model representing an order item.
/// </summary>
public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
}

/// <summary>
/// Test model with nullable properties.
/// </summary>
public class NullableModel
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public int? Age { get; set; }
    public DateTime? CreatedDate { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Test model for projection scenarios.
/// </summary>
public class PersonDto
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsAdult => Age >= 18;
}

/// <summary>
/// Test model for anonymous type projections.
/// </summary>
public class PersonSummary
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Age { get; set; }
}
