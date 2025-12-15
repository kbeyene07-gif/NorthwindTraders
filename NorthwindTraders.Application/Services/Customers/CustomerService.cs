using Microsoft.EntityFrameworkCore;
using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Application.Dtos.Orders;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Application.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly INorthwindDbContext _context;

        public CustomerService(INorthwindDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CustomerDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Customers.AsNoTracking();

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    City = c.City,
                    Country = c.Country,
                    Address1 = c.Address1,
                    Address2 = c.Address2,
                    State = c.State,
                    ZipCode = c.ZipCode,
                    Phone = c.Phone,
                    CreatedAtUtc = c.CreatedAtUtc,
                    UpdatedAtUtc = c.UpdatedAtUtc
                })
                .ToListAsync(ct);

            return new PagedResult<CustomerDto>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    City = c.City,
                    Country = c.Country,
                    Address1 = c.Address1,
                    Address2 = c.Address2,
                    State = c.State,
                    ZipCode = c.ZipCode,
                    Phone = c.Phone,
                    CreatedAtUtc = c.CreatedAtUtc,
                    UpdatedAtUtc = c.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<CustomerWithOrdersDto?> GetWithOrdersAsync(int id, CancellationToken ct = default)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (customer == null)
                return null;

            return new CustomerWithOrdersDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                City = customer.City,
                Country = customer.Country,
                Address1 = customer.Address1,
                Address2 = customer.Address2,
                State = customer.State,
                ZipCode = customer.ZipCode,
                Phone = customer.Phone,
                Orders = customer.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        CustomerId = o.CustomerId,
                        CustomerName = customer.FirstName + " " + customer.LastName,
                        TotalAmount = o.TotalAmount,
                        CreatedAtUtc = o.CreatedAtUtc,
                        UpdatedAtUtc = o.UpdatedAtUtc
                    })
                    .ToList()
            };
        }

        public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var entity = new Domain.Models.Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                City = dto.City,
                Country = dto.Country,
                Address1 = dto.Address1,
                Address2 = dto.Address2,
                State = dto.State,
                ZipCode = dto.ZipCode,
                Phone = dto.Phone,
                CreatedAtUtc = now
            };

            _context.Customers.Add(entity);
            await _context.SaveChangesAsync(ct);

            return new CustomerDto
            {
                Id = entity.Id,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                City = entity.City,
                Country = entity.Country,
                Address1 = entity.Address1,
                Address2 = entity.Address2,
                State = entity.State,
                ZipCode = entity.ZipCode,
                Phone = entity.Phone,
                CreatedAtUtc = entity.CreatedAtUtc,
                UpdatedAtUtc = entity.UpdatedAtUtc
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateCustomerDto dto, CancellationToken ct = default)
        {
            var entity = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null)
                return false;

            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;
            entity.City = dto.City;
            entity.Country = dto.Country;
            entity.Address1 = dto.Address1;
            entity.Address2 = dto.Address2;
            entity.State = dto.State;
            entity.ZipCode = dto.ZipCode;
            entity.Phone = dto.Phone;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (entity == null)
                return false;

            _context.Customers.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
