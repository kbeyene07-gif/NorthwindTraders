using System.ComponentModel.DataAnnotations;

namespace NorthwindTraders.Application.Dtos.OrderItems
{
    public class UpdateOrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
