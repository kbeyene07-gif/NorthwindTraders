using System.ComponentModel.DataAnnotations;

namespace NorthwindTraders.Application.Dtos.Suppliers
{
    public class UpdateSupplierDto
    {
        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = default!;

        [MaxLength(100)]
        public string? ContactName { get; set; }

        [MaxLength(100)]
        public string? ContactTitle { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? Fax { get; set; }
    }
}
