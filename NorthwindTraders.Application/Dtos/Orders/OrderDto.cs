namespace NorthwindTraders.Application.Dtos.Orders
{
    public class OrderDto
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = default!;
        public DateTime OrderDate { get; set; }

        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
