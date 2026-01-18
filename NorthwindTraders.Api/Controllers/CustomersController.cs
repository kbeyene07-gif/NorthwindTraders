using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Application.Services.Customers;

namespace NorthwindTraders.Api.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/customers")]
    //[Authorize(Policy = AuthScopes.CustomersRead)]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomersController(ICustomerService service)
        {
            _service = service;
        }

        // GET: api/v1/customers?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, ct);
            return Ok(result);
        }

        // GET: api/v1/customers/5
        // Name the route so POST can reference it safely
        [HttpGet("{id:int}", Name = "Customers_GetById")]
        public async Task<IActionResult> GetCustomer(int id, CancellationToken ct = default)
        {
            var customer = await _service.GetByIdAsync(id, ct);
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        // GET: api/v1/customers/5/orders
        [HttpGet("{id:int}/orders")]
        public async Task<IActionResult> GetCustomerWithOrders(int id, CancellationToken ct = default)
        {
            var customer = await _service.GetWithOrdersAsync(id, ct);
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        // POST: api/v1/customers
        [HttpPost]
        [Authorize(Policy = AuthScopes.CustomersWrite)]
        public async Task<IActionResult> CreateCustomer(
    [FromBody] CreateCustomerDto dto,
    CancellationToken ct = default)
        {
            var created = await _service.CreateAsync(dto, ct);

            if (created is null)
                return Problem(
                    title: "Customer creation failed",
                    statusCode: StatusCodes.Status500InternalServerError);

            // Unit-test safe (RouteData can be null in controller unit tests)
            var version = ControllerContext?.RouteData?.Values?["version"]?.ToString() ?? "1.0";

            return CreatedAtRoute(
                routeName: "Customers_GetById",
                routeValues: new { version, id = created.Id },
                value: created
            );
        }

        // PUT: api/v1/customers/5
        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthScopes.CustomersWrite)]
        public async Task<IActionResult> UpdateCustomer(
            int id,
            [FromBody] UpdateCustomerDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var success = await _service.UpdateAsync(id, dto, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/v1/customers/5
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthScopes.CustomersWrite)]
        public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct = default)
        {
            var success = await _service.DeleteAsync(id, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
