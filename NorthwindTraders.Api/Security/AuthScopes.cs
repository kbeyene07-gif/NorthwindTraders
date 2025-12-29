namespace NorthwindTraders.Api.Security
{

        // Auth0 scopes defined for your API
        public static class AuthScopes
        {
            public const string CustomersRead = "read:customers";
            public const string CustomersWrite = "write:customers";

            public const string OrdersRead = "read:orders";
            public const string OrdersWrite = "write:orders";

            public const string ProductsRead = "read:products";
            public const string ProductsWrite = "write:products";
           
            public const string OrderItemsRead = "read:orderItems";
            public const string OrderItemsWrite = "write:orderItems";

    }
    }

