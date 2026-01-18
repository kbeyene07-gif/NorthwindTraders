using System.ComponentModel.DataAnnotations;

namespace NorthwindTraders.Application.Dtos.OrderItems
{
    public class CreateOrderItemDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
