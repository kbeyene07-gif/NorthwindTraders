using NorthwindTraders.Application.Common;
using NorthwindTraders.Application.Dtos.OrderItems;
using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Application.Services.OrderItems
{
    public interface IOrderItemService
    {
        Task<PagedResult<OrderItemDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            int? orderId = null,
            CancellationToken ct = default);

        Task<OrderItemDto?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<OrderItemDto> CreateAsync(CreateOrderItemDto dto, CancellationToken ct = default);

        Task<bool> UpdateAsync(int id, UpdateOrderItemDto dto, CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
