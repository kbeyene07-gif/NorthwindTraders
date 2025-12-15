using System.ComponentModel.DataAnnotations;

namespace NorthwindTraders.Application.Dtos.Customers
{
    public class CreateCustomerDto
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = default!;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = default!;

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(200)]
        public string? Address1 { get; set; }

        [MaxLength(200)]
        public string? Address2 { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? ZipCode { get; set; }

        [Phone]
        public string? Phone { get; set; }
    }
}
