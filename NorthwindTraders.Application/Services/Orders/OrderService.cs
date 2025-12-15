using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Dtos.OrderItems;
using NorthwindTraders.Application.Dtos.Orders;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Api.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly INorthwindDbContext _context;

        public OrderService(INorthwindDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<OrderDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenBy(o => o.OrderNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                    TotalAmount = o.TotalAmount,
                    CreatedAtUtc = o.CreatedAtUtc,
                    UpdatedAtUtc = o.UpdatedAtUtc
                })
                .ToListAsync(ct);

            return new PagedResult<OrderDto>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Where(o => o.Id == id)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                    TotalAmount = o.TotalAmount,
                    CreatedAtUtc = o.CreatedAtUtc,
                    UpdatedAtUtc = o.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<OrderWithItemsDto?> GetWithItemsAsync(int id, CancellationToken ct = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)              // 👈 use OrderItems here
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order == null)
                return null;

            return new OrderWithItemsDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FirstName + " " + order.Customer.LastName,
                TotalAmount = order.TotalAmount,

                // 👇 use OrderItems here too
                Items = order.OrderItems.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList()
            };
        }

        public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct = default)
        {
            // 1) Validate input (you can also enforce this with FluentValidation)
            if (dto.TotalAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(dto.TotalAmount), "TotalAmount cannot be negative.");

            // 2) Make sure Customer exists (enterprise: don’t create orphan orders)
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == dto.CustomerId)
                .Select(c => new { c.Id, c.FirstName, c.LastName })
                .FirstOrDefaultAsync(ct);

            if (customer is null)
                throw new InvalidOperationException($"Customer '{dto.CustomerId}' was not found.");

            var now = DateTime.UtcNow;

            // 3) Create the entity (only server-controlled fields are set here)
            var entity = new Domain.Models.Order
            {
                OrderNumber = dto.OrderNumber,
                OrderDate = dto.OrderDate,
                CustomerId = dto.CustomerId,
                TotalAmount = dto.TotalAmount,
                CreatedAtUtc = now
            };

            _context.Orders.Add(entity);
            await _context.SaveChangesAsync(ct);

            // 4) Return DTO without EF Entry() / explicit loading
            return new OrderDto
            {
                Id = entity.Id,
                OrderNumber = entity.OrderNumber,
                OrderDate = entity.OrderDate,
                CustomerId = entity.CustomerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                TotalAmount = entity.TotalAmount,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc
            };
        }


        public async Task<bool> UpdateAsync(int id, UpdateOrderDto dto, CancellationToken ct = default)
        {
            var entity = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (entity == null)
                return false;

            entity.OrderNumber = dto.OrderNumber;
            entity.OrderDate = dto.OrderDate;
            entity.TotalAmount = dto.TotalAmount;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (entity == null)
                return false;

            _context.Orders.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
