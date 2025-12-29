using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Dtos.Products;
using NorthwindTraders.Application.Products.Queries;
using NorthwindTraders.Domain.Common;
using NorthwindTraders.Domain.Models;

namespace NorthwindTraders.Application.Services.Products
{
    public class ProductService : IProductService
    {
        private const int MaxPageSize = 100;

        private readonly INorthwindDbContext _context;

        public ProductService(INorthwindDbContext context)
        {
            _context = context;
        }

        // NEW enterprise catalog query
        public async Task<PagedResult<ProductDto>> GetCatalogAsync(ProductQuery query, CancellationToken ct = default)
        {
            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

            if (pageSize > MaxPageSize)
                throw new ArgumentException($"PageSize cannot exceed {MaxPageSize}.", nameof(query.PageSize));

            if (query.MinPrice is not null && query.MinPrice < 0)
                throw new ArgumentException("MinPrice cannot be negative.", nameof(query.MinPrice));

            if (query.MaxPrice is not null && query.MaxPrice < 0)
                throw new ArgumentException("MaxPrice cannot be negative.", nameof(query.MaxPrice));

            if (query.MinPrice is not null && query.MaxPrice is not null && query.MinPrice > query.MaxPrice)
                throw new ArgumentException("MinPrice cannot be greater than MaxPrice.");

            var sortBy = (query.SortBy ?? "name").Trim().ToLowerInvariant();
            var sortDir = (query.SortDir ?? "asc").Trim().ToLowerInvariant();

            if (sortDir is not ("asc" or "desc"))
                throw new ArgumentException("SortDir must be 'asc' or 'desc'.", nameof(query.SortDir));

            var baseQuery = _context.Products
                .AsNoTracking()
                .Include(p => p.Supplier)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                baseQuery = baseQuery.Where(p => p.ProductName.Contains(term));
            }

            if (query.SupplierId is not null)
            {
                if (query.SupplierId <= 0)
                    throw new ArgumentException("SupplierId must be a positive integer.", nameof(query.SupplierId));

                baseQuery = baseQuery.Where(p => p.SupplierId == query.SupplierId);
            }

            if (query.MinPrice is not null)
                baseQuery = baseQuery.Where(p => p.UnitPrice >= query.MinPrice);

            if (query.MaxPrice is not null)
                baseQuery = baseQuery.Where(p => p.UnitPrice <= query.MaxPrice);

            if (query.Discontinued is not null)
                baseQuery = baseQuery.Where(p => p.IsDiscontinued == query.Discontinued);

            // Sorting
            baseQuery = (sortBy, sortDir) switch
            {
                ("price", "asc") => baseQuery.OrderBy(p => p.UnitPrice).ThenBy(p => p.ProductName),
                ("price", "desc") => baseQuery.OrderByDescending(p => p.UnitPrice).ThenBy(p => p.ProductName),

                ("createdat", "asc") => baseQuery.OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.ProductName),
                ("createdat", "desc") => baseQuery.OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.ProductName),

                ("name", "desc") => baseQuery.OrderByDescending(p => p.ProductName),
                ("name", "asc") => baseQuery.OrderBy(p => p.ProductName),

                _ => throw new ArgumentException("SortBy must be one of: name, price, createdAt.", nameof(query.SortBy))
            };

            var totalCount = await baseQuery.CountAsync(ct);

            var items = await baseQuery
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

        // Backwards compatible method (old signature)
        public Task<PagedResult<ProductDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
            => GetCatalogAsync(new ProductQuery { PageNumber = pageNumber, PageSize = pageSize }, ct);

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
            if (string.IsNullOrWhiteSpace(dto.ProductName))
                throw new ArgumentException("ProductName is required.", nameof(dto.ProductName));

            if (dto.UnitPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(dto.UnitPrice), "UnitPrice cannot be negative.");

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

            // Basic guardrails
            if (string.IsNullOrWhiteSpace(dto.ProductName))
                throw new ArgumentException("ProductName is required.", nameof(dto.ProductName));

            if (dto.UnitPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(dto.UnitPrice), "UnitPrice cannot be negative.");

            entity.ProductName = dto.ProductName.Trim();
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
