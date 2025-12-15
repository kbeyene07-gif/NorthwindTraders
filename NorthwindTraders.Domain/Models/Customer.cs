using NorthwindTraders.Domain.Common;
using NorthwindTraders.Domain.Models.Identity;


namespace NorthwindTraders.Domain.Models
{
    public class Customer : AuditableEntity
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

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
