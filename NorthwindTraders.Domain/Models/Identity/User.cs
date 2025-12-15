using System;
using System.Collections.Generic;

namespace NorthwindTraders.Domain.Models.Identity
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Email { get; set; } = default!;
        public string? DisplayName { get; set; }

        public int UserTypeId { get; set; }
        public UserType UserType { get; set; } = default!;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
