
using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Dtos.Products;
using NorthwindTraders.Domain.Common;
using NorthwindTraders.Domain.Models;
namespace NorthwindTraders.Application.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly INorthwindDbContext _context;

        public ProductService(INorthwindDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ProductDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Supplier);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(p => p.ProductName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.CompanyName,
                    UnitPrice = p.UnitPrice,
                    Package = p.Package,
                    IsDiscontinued = p.IsDiscontinued,
                    CreatedAtUtc = p.CreatedAtUtc,
                    UpdatedAtUtc = p.UpdatedAtUtc
                })
                .ToListAsync(ct);

            return new PagedResult<ProductDto>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier.CompanyName,
                    UnitPrice = p.UnitPrice,
                    Package = p.Package,
                    IsDiscontinued = p.IsDiscontinued,
                    CreatedAtUtc = p.CreatedAtUtc,
                    UpdatedAtUtc = p.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
        {
            // Basic guardrails (better with FluentValidation, but this is still enterprise-friendly)
            if (string.IsNullOrWhiteSpace(dto.ProductName))
                throw new ArgumentException("ProductName is required.", nameof(dto.ProductName));

            if (dto.UnitPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(dto.UnitPrice), "UnitPrice cannot be negative.");

            // Validate supplier exists + get supplier name for DTO (no Entry()/LoadAsync)
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Id == dto.SupplierId)
                .Select(s => new { s.Id, s.CompanyName })
                .FirstOrDefaultAsync(ct);

            if (supplier is null)
                throw new InvalidOperationException($"Supplier '{dto.SupplierId}' was not found.");

            var now = DateTime.UtcNow;

            var entity = new Product
            {
                ProductName = dto.ProductName.Trim(),
                SupplierId = dto.SupplierId,
                UnitPrice = dto.UnitPrice,
                Package = dto.Package,
                IsDiscontinued = dto.IsDiscontinued,
                CreatedAtUtc = now
            };

            _context.Products.Add(entity);
            await _context.SaveChangesAsync(ct);

            return new ProductDto
            {
                Id = entity.Id,
                ProductName = entity.ProductName,
                SupplierId = entity.SupplierId,
                SupplierName = supplier.CompanyName,
                UnitPrice = entity.UnitPrice,
                Package = entity.Package,
                IsDiscontinued = entity.IsDiscontinued,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
        {
            var entity = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null)
                return false;

            entity.ProductName = dto.ProductName;
            entity.SupplierId = dto.SupplierId;
            entity.UnitPrice = dto.UnitPrice;
            entity.Package = dto.Package;
            entity.IsDiscontinued = dto.IsDiscontinued;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null)
                return false;

            _context.Products.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}

