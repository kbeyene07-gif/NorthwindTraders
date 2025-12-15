
using NorthwindTraders.Domain.Models.Identity;

namespace NorthwindTraders.Domain.Common
{
    public abstract class AuditableEntity
    {
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public Guid? UpdatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
    }
}
