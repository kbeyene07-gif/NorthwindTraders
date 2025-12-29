using NorthwindTraders.Application.Dtos.Products;
using NorthwindTraders.Application.Products.Queries;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Application.Services.Products
{
    public interface IProductService
    {
        // Enterprise catalog query (new)
        Task<PagedResult<ProductDto>> GetCatalogAsync(ProductQuery query, CancellationToken ct = default);

        // Backwards compatible (old) - now just calls the catalog query internally
        Task<PagedResult<ProductDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);

        Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
