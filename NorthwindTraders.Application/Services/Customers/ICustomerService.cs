

using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Application.Services.Customers
{
    public interface ICustomerService
    {
        Task<PagedResult<CustomerDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);

        Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<CustomerWithOrdersDto?> GetWithOrdersAsync(int id, CancellationToken ct = default);

        Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default);

        Task<bool> UpdateAsync(int id, UpdateCustomerDto dto, CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
