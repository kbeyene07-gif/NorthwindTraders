using FluentAssertions;
using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Application.Services.Customers;
using NorthwindTraders.Domain.Models;
using NorthwindTraders.Tests.Unit.TestHelpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NorthwindTraders.Tests.Unit.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistCustomer_AndReturnDto()
    {
        // Arrange
        await using var db = DbContextTestFactory.CreateInMemory();
        var service = new CustomerService(db);

        var dto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            City = "Seattle",
            Country = "USA",
            Address1 = "1 Main St",
            Address2 = null,
            State = "WA",
            ZipCode = "98101",
            Phone = "555-1111"
        };

        // Act
        var created = await service.CreateAsync(dto);

        // Assert
        created.Id.Should().BeGreaterThan(0);
        created.FirstName.Should().Be("John");
        created.LastName.Should().Be("Doe");
        created.CreatedAtUtc.Should().NotBe(default);

        var inDb = db.Customers.Single(c => c.Id == created.Id);
        inDb.FirstName.Should().Be("John");
        inDb.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerNotFound_ShouldReturnFalse()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        var service = new CustomerService(db);

        var dto = new UpdateCustomerDto
        {
            FirstName = "New",
            LastName = "Name",
            City = "X",
            Country = "Y",
            Address1 = "A1",
            Address2 = "A2",
            State = "S",
            ZipCode = "Z",
            Phone = "P"
        };

        var ok = await service.UpdateAsync(id: 999, dto);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerExists_ShouldUpdateFields_AndSetUpdatedAtUtc()
    {
        // Arrange
        await using var db = DbContextTestFactory.CreateInMemory();
        db.Customers.Add(new Customer
        {
            FirstName = "Old",
            LastName = "Customer",
            City = "OldCity",
            Country = "OldCountry",
            Address1 = "OldAddr",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        });
        await db.SaveChangesAsync();

        var existing = db.Customers.Single();
        existing.UpdatedAtUtc.Should().BeNull();

        var service = new CustomerService(db);

        var dto = new UpdateCustomerDto
        {
            FirstName = "New",
            LastName = "Customer",
            City = "NewCity",
            Country = "USA",
            Address1 = "1 Main",
            Address2 = "Unit 2",
            State = "WA",
            ZipCode = "98101",
            Phone = "555-2222"
        };

        // Act
        var ok = await service.UpdateAsync(existing.Id, dto);

        // Assert
        ok.Should().BeTrue();

        var updated = db.Customers.Single(c => c.Id == existing.Id);
        updated.FirstName.Should().Be("New");
        updated.City.Should().Be("NewCity");
        updated.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenCustomerExists_ShouldRemoveAndReturnTrue()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        db.Customers.Add(new Customer { FirstName = "To", LastName = "Delete", CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var id = db.Customers.Single().Id;
        var service = new CustomerService(db);

        var ok = await service.DeleteAsync(id);

        ok.Should().BeTrue();
        db.Customers.Any(c => c.Id == id).Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnCorrectPage_AndTotalCount()
    {
        await using var db = DbContextTestFactory.CreateInMemory();
        for (int i = 1; i <= 25; i++)
        {
            db.Customers.Add(new Customer
            {
                FirstName = $"F{i}",
                LastName = $"L{i}",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var service = new CustomerService(db);

        var result = await service.GetPagedAsync(pageNumber: 2, pageSize: 10);

        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(10);
    }
}
