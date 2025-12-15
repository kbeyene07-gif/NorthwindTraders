

using NorthwindTraders.Application.Dtos.Products;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Application.Services.Products
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
        Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
