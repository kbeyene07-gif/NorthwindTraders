

using NorthwindTraders.Domain.Common;

namespace NorthwindTraders.Domain.Models
{
    public class Product : AuditableEntity
    {
        public int Id { get; set; }

        public string ProductName { get; set; } = default!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = default!;

        public decimal UnitPrice { get; set; }
        public string? Package { get; set; }
        public bool IsDiscontinued { get; set; }

        // 👇 This matches Product -> OrderItems config
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
