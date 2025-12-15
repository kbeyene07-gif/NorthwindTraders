//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using NorthwindTraders.Api.Data;
//using NorthwindTraders.Api.Models;

//namespace NorthwindTraders.Api.Controllers
//{
//    [ApiController]
//    [ApiVersion("1.0")]
//    [Route("api/v{version:apiVersion}/[controller]")]
//    public class OrderItemsController : ControllerBase
//    {
//        private readonly NorthwindTradersContext _context;

//        public OrderItemsController(NorthwindTradersContext context)
//        {
//            _context = context;
//        }

//        // GET: api/OrderItems
//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<OrderItem>>> GetOrderItems()
//        {
//            var orderItems = await _context.OrderItems
//                .Include(oi => oi.Order)
//                .Include(oi => oi.Product)
//                .ToListAsync();

//            return Ok(orderItems);
//        }

//        // GET: api/OrderItems/5
//        [HttpGet("{id:int}")]
//        public async Task<ActionResult<OrderItem>> GetOrderItem(int id)
//        {
//            var orderItem = await _context.OrderItems
//                .Include(oi => oi.Order)
//                .Include(oi => oi.Product)
//                .FirstOrDefaultAsync(oi => oi.Id == id);

//            if (orderItem == null)
//                return NotFound();

//            return Ok(orderItem);
//        }

//        // POST: api/OrderItems
//        [HttpPost]
//        public async Task<ActionResult<OrderItem>> CreateOrderItem(OrderItem orderItem)
//        {
//            // You can validate OrderId and ProductId exist if you want.
//            _context.OrderItems.Add(orderItem);
//            await _context.SaveChangesAsync();

//            return CreatedAtAction(nameof(GetOrderItem), new { id = orderItem.Id }, orderItem);
//        }

//        // PUT: api/OrderItems/5
//        [HttpPut("{id:int}")]
//        public async Task<IActionResult> UpdateOrderItem(int id, OrderItem orderItem)
//        {
//            if (id != orderItem.Id)
//                return BadRequest("Id in URL and body must match.");

//            _context.Entry(orderItem).State = EntityState.Modified;

//            try
//            {
//                await _context.SaveChangesAsync();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                var exists = await _context.OrderItems.AnyAsync(oi => oi.Id == id);
//                if (!exists)
//                    return NotFound();

//                throw;
//            }

//            return NoContent();
//        }

//        // DELETE: api/OrderItems/5
//        [HttpDelete("{id:int}")]
//        public async Task<IActionResult> DeleteOrderItem(int id)
//        {
//            var orderItem = await _context.OrderItems.FindAsync(id);
//            if (orderItem == null)
//                return NotFound();

//            _context.OrderItems.Remove(orderItem);
//            await _context.SaveChangesAsync();

//            return NoContent();
//        }
//    }
//}

