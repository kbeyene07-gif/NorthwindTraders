using System.ComponentModel.DataAnnotations;

namespace NorthwindTraders.Application.Dtos.Products
{
    public class CreateProductDto
    {
        [Required, MaxLength(200)]
        public string ProductName { get; set; } = default!;

        [Required]
        public int SupplierId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [MaxLength(100)]
        public string? Package { get; set; }

        public bool IsDiscontinued { get; set; }
    }
}
