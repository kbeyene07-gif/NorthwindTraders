
using NorthwindTraders.Application.Dtos.OrderItems;

namespace NorthwindTraders.Application.Dtos.Orders
{
    public class OrderWithItemsDto
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = default!;
        public DateTime OrderDate { get; set; }

        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public decimal TotalAmount { get; set; }

        public IEnumerable<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
