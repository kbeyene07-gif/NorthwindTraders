using System.ComponentModel.DataAnnotations;

namespace NorthwindTraders.Application.Dtos.Orders
{
    public class UpdateOrderDto
    {
        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = default!;

        [Required]
        public DateTime OrderDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }
    }
}
