using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Dtos.OrderItems;
using NorthwindTraders.Domain.Common;
using NorthwindTraders.Domain.Models;

namespace NorthwindTraders.Application.Services.OrderItems
{
    public class OrderItemService : IOrderItemService
    {
        private readonly INorthwindDbContext _context;

        public OrderItemService(INorthwindDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<OrderItemDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            int? orderId = null,
            CancellationToken ct = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.OrderItems
                .AsNoTracking()
                .Include(i => i.Product)
                .AsQueryable();

            if (orderId.HasValue)
            {
                query = query.Where(i => i.OrderId == orderId.Value);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(i => i.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                })
                .ToListAsync(ct);

            return new PagedResult<OrderItemDto>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<OrderItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.OrderItems
                .AsNoTracking()
                .Include(i => i.Product)
                .Where(i => i.Id == id)
                .Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<OrderItemDto> CreateAsync(CreateOrderItemDto dto, CancellationToken ct = default)
        {
            // Enterprise guardrails: no orphan rows
            var orderExists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.Id == dto.OrderId, ct);

            if (!orderExists)
                throw new InvalidOperationException($"Order '{dto.OrderId}' was not found.");

            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == dto.ProductId)
                .Select(p => new { p.Id, p.ProductName })
                .FirstOrDefaultAsync(ct);

            if (product is null)
                throw new InvalidOperationException($"Product '{dto.ProductId}' was not found.");

            if (dto.Quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(dto.Quantity), "Quantity must be >= 1.");

            if (dto.UnitPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(dto.UnitPrice), "UnitPrice cannot be negative.");

            var entity = new OrderItem
            {
                OrderId = dto.OrderId,
                ProductId = dto.ProductId,
                UnitPrice = dto.UnitPrice,
                Quantity = dto.Quantity
            };

            _context.OrderItems.Add(entity);
            await _context.SaveChangesAsync(ct);

            return new OrderItemDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = product.ProductName,
                UnitPrice = entity.UnitPrice,
                Quantity = entity.Quantity
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateOrderItemDto dto, CancellationToken ct = default)
        {
            var entity = await _context.OrderItems
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (entity == null)
                return false;

            var productExists = await _context.Products
                .AsNoTracking()
                .AnyAsync(p => p.Id == dto.ProductId, ct);

            if (!productExists)
                throw new InvalidOperationException($"Product '{dto.ProductId}' was not found.");

            if (dto.Quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(dto.Quantity), "Quantity must be >= 1.");

            if (dto.UnitPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(dto.UnitPrice), "UnitPrice cannot be negative.");

            entity.ProductId = dto.ProductId;
            entity.UnitPrice = dto.UnitPrice;
            entity.Quantity = dto.Quantity;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.OrderItems
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (entity == null)
                return false;

            _context.OrderItems.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
