
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace NorthwindTraders.Api.Security
{
    public static class AuthorizationPolicies
    {
        public const string ProductsWriteOrAdmin = "ProductsWriteOrAdmin";

        public static void AddAppPolicies(AuthorizationOptions options)
        {
            // Existing ones…
            AddScopePolicy(options, AuthScopes.CustomersRead);
            AddScopePolicy(options, AuthScopes.CustomersWrite);
            AddScopePolicy(options, AuthScopes.OrdersRead);
            AddScopePolicy(options, AuthScopes.OrdersWrite);

            AddScopePolicy(options, AuthScopes.OrderItemsRead);
            AddScopePolicy(options, AuthScopes.OrderItemsWrite);
            // Products: simple scope policies
            AddScopePolicy(options, AuthScopes.ProductsRead);
            AddScopePolicy(options, AuthScopes.ProductsWrite);

            // Role-based
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            // ⭐ Mixed: write:products OR Admin
            options.AddPolicy(ProductsWriteOrAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var user = context.User;

                    var hasWriteScope = HasScope(user, AuthScopes.ProductsWrite);
                    var isAdmin = user.IsInRole("Admin");

                    // OR logic
                    return hasWriteScope || isAdmin;
                });
            });
        }

        private static void AddScopePolicy(AuthorizationOptions options, string scope)
        {
            options.AddPolicy(scope, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => HasScope(context.User, scope));
            });
        }

        private static bool HasScope(ClaimsPrincipal user, string scope)
        {
            var scopeClaim = user.FindFirst("scope")?.Value;
            if (string.IsNullOrEmpty(scopeClaim))
                return false;

            var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains(scope);
        }
    }
}
