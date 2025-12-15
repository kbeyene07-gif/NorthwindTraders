namespace NorthwindTraders.Application.Dtos.Suppliers
{
    public class SupplierDto
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = default!;
        public string? ContactName { get; set; }
        public string? ContactTitle { get; set; }

        public string? City { get; set; }
        public string? Country { get; set; }

        public string? Phone { get; set; }
        public string? Fax { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}

