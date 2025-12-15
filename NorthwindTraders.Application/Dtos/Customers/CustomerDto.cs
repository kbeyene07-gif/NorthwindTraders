namespace NorthwindTraders.Application.Dtos.Customers
{
    public class CustomerDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;

        public string? City { get; set; }
        public string? Country { get; set; }

        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        public string? Phone { get; set; }

        // audit (read-only)
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
