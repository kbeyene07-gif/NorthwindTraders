

using NorthwindTraders.Application.Dtos.Orders;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Application.Services.Orders
{
    public interface IOrderService
    {
        Task<PagedResult<OrderDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default);
        Task<OrderDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<OrderWithItemsDto?> GetWithItemsAsync(int id, CancellationToken ct = default);
        Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct = default);
        Task<bool> UpdateAsync(int id, UpdateOrderDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
