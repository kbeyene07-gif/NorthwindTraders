namespace NorthwindTraders.Application.Dtos.Products
{
    public class ProductDto
    {
        public int Id { get; set; }

        public string ProductName { get; set; } = default!;

        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public decimal UnitPrice { get; set; }
        public string? Package { get; set; }
        public bool IsDiscontinued { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
