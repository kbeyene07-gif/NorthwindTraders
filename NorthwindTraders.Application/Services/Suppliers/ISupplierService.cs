using NorthwindTraders.Application.Dtos.Suppliers;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Application.Services.Suppliers
{
    public interface ISupplierService
    {
        Task<PagedResult<SupplierDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
        Task<SupplierDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<SupplierDto> CreateAsync(CreateSupplierDto dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateSupplierDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
