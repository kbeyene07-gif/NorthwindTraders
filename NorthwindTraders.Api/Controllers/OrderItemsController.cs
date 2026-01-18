using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NorthwindTraders.Api.Security;
using NorthwindTraders.Application.Dtos.OrderItems;
using NorthwindTraders.Application.Services.OrderItems;

namespace NorthwindTraders.Api.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = AuthScopes.OrderItemsRead)]
    public class OrderItemsController : ControllerBase
    {
        private readonly IOrderItemService _service;

        public OrderItemsController(IOrderItemService service)
        {
            _service = service;
        }

        // GET: api/v1/orderitems?pageNumber=1&pageSize=20&orderId=10
        [HttpGet]
        public async Task<IActionResult> GetOrderItems(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? orderId = null,
            CancellationToken ct = default)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize, orderId, ct);
            return Ok(result);
        }

        // GET: api/v1/orderitems/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderItem(int id, CancellationToken ct = default)
        {
            var item = await _service.GetByIdAsync(id, ct);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        // POST: api/v1/orderitems
        [HttpPost]
        [Authorize(Policy = AuthScopes.OrderItemsWrite)]
        public async Task<IActionResult> CreateOrderItem(
            [FromBody] CreateOrderItemDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var created = await _service.CreateAsync(dto, ct);

            return CreatedAtAction(
                nameof(GetOrderItem),
                new { id = created.Id, version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0" },
                created);
        }

        // PUT: api/v1/orderitems/5
        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthScopes.OrderItemsWrite)]
        public async Task<IActionResult> UpdateOrderItem(
            int id,
            [FromBody] UpdateOrderItemDto dto,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var success = await _service.UpdateAsync(id, dto, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/v1/orderitems/5
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthScopes.OrderItemsWrite)]
        public async Task<IActionResult> DeleteOrderItem(int id, CancellationToken ct = default)
        {
            var success = await _service.DeleteAsync(id, ct);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
