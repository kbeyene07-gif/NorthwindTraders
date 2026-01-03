using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NorthwindTraders.Api.Controllers.v1;
using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Application.Services.Customers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NorthwindTraders.Tests.Unit.Controllers;

public class CustomersControllerTests
{
    [Fact]
    public async Task GetCustomer_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var svc = new Mock<ICustomerService>();
        svc.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>()))
           .ReturnsAsync((CustomerDto?)null);

        var controller = new CustomersController(svc.Object);

        // Act
        var result = await controller.GetCustomer(5, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateCustomer_ShouldReturn201_CreatedAtAction()
    {
        // Arrange
        var svc = new Mock<ICustomerService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<CreateCustomerDto>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new CustomerDto { Id = 123, FirstName = "A", LastName = "B", CreatedAtUtc = DateTime.UtcNow });

        var controller = new CustomersController(svc.Object);

        // Needed because my action reads HttpContext (for api version)
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var dto = new CreateCustomerDto
        {
            FirstName = "A",
            LastName = "B",
            City = "X",
            Country = "Y",
            Address1 = "Addr",
            Address2 = null,
            State = "S",
            ZipCode = "Z",
            Phone = "P"
        };

        // Act
        var result = await controller.CreateCustomer(dto, CancellationToken.None);

        // Assert
        var created = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        created.RouteName.Should().Be("Customers_GetById");

        var body = created.Value.Should().BeOfType<CustomerDto>().Subject;
        body.Id.Should().Be(123);
    }
}
