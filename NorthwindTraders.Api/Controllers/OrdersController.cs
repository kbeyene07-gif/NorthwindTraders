using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Api.Services.Orders;
using NorthwindTraders.Application.Dtos.Orders;


namespace NorthwindTraders.Api.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = AuthScopes.OrdersRead)]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service)
        {
            _service = service;
        }

        // GET: api/v1/orders?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, ct);
            return Ok(result);
        }

        // GET: api/v1/orders/10
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrder(int id, CancellationToken ct = default)
        {
            var order = await _service.GetByIdAsync(id, ct);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        // GET: api/v1/orders/10/items
        [HttpGet("{id:int}/items")]
        public async Task<IActionResult> GetOrderWithItems(int id, CancellationToken ct = default)
        {
            var order = await _service.GetWithItemsAsync(id, ct);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        // POST: api/v1/orders
        [HttpPost]
        [Authorize(Policy = AuthScopes.OrdersWrite)]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreateOrderDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var created = await _service.CreateAsync(dto, ct);

            return CreatedAtAction(
                nameof(GetOrder),
                new { id = created.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
                created);
        }

        // PUT: api/v1/orders/10
        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthScopes.OrdersWrite)]
        public async Task<IActionResult> UpdateOrder(
            int id,
            [FromBody] UpdateOrderDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var success = await _service.UpdateAsync(id, dto, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/v1/orders/10
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthScopes.OrdersWrite)]

        public async Task<IActionResult> DeleteOrder(int id, CancellationToken ct = default)
        {
            var success = await _service.DeleteAsync(id, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
