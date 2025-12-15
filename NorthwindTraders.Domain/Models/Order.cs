

using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Domain.Models
{
    public class Order : AuditableEntity
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = default!;
        public DateTime OrderDate { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;

        public decimal TotalAmount { get; set; }

        // 👇 Use this name to match DbContext config
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}