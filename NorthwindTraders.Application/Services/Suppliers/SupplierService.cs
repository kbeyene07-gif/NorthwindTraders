
using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Dtos.Suppliers;
using NorthwindTraders.Domain.Common;
using NorthwindTraders.Domain.Models;

namespace NorthwindTraders.Application.Services.Suppliers
{
    public class SupplierService : ISupplierService
    {
        private readonly INorthwindDbContext _context;

        public SupplierService(INorthwindDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<SupplierDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Suppliers.AsNoTracking();

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(s => s.CompanyName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    CompanyName = s.CompanyName,
                    ContactName = s.ContactName,
                    ContactTitle = s.ContactTitle,
                    City = s.City,
                    Country = s.Country,
                    Phone = s.Phone,
                    Fax = s.Fax,
                    CreatedAtUtc = s.CreatedAtUtc,
                    UpdatedAtUtc = s.UpdatedAtUtc
                })
                .ToListAsync(ct);

            return new PagedResult<SupplierDto>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<SupplierDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SupplierDto
                {
                    Id = s.Id,
                    CompanyName = s.CompanyName,
                    ContactName = s.ContactName,
                    ContactTitle = s.ContactTitle,
                    City = s.City,
                    Country = s.Country,
                    Phone = s.Phone,
                    Fax = s.Fax,
                    CreatedAtUtc = s.CreatedAtUtc,
                    UpdatedAtUtc = s.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var entity = new Supplier
            {
                CompanyName = dto.CompanyName,
                ContactName = dto.ContactName,
                ContactTitle = dto.ContactTitle,
                City = dto.City,
                Country = dto.Country,
                Phone = dto.Phone,
                Fax = dto.Fax,
                CreatedAtUtc = now
            };

            _context.Suppliers.Add(entity);
            await _context.SaveChangesAsync(ct);

            return new SupplierDto
            {
                Id = entity.Id,
                CompanyName = entity.CompanyName,
                ContactName = entity.ContactName,
                ContactTitle = entity.ContactTitle,
                City = entity.City,
                Country = entity.Country,
                Phone = entity.Phone,
                Fax = entity.Fax,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateSupplierDto dto, CancellationToken ct = default)
        {
            var entity = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity == null)
                return false;

            entity.CompanyName = dto.CompanyName;
            entity.ContactName = dto.ContactName;
            entity.ContactTitle = dto.ContactTitle;
            entity.City = dto.City;
            entity.Country = dto.Country;
            entity.Phone = dto.Phone;
            entity.Fax = dto.Fax;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity == null)
                return false;

            _context.Suppliers.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
