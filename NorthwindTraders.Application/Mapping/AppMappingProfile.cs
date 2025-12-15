

using AutoMapper;
using NorthwindTraders.Application.Dtos.Customers;
using NorthwindTraders.Application.Dtos.OrderItems;
using NorthwindTraders.Application.Dtos.Orders;
using NorthwindTraders.Application.Dtos.Products;
using NorthwindTraders.Application.Dtos.Suppliers;
using NorthwindTraders.Domain.Models;

namespace NorthwindTraders.Application.Mapping
{
    public class AppMappingProfile : Profile
    {
        public AppMappingProfile()
        {
            // ---------------- Customers ----------------

            CreateMap<Customer, CustomerDto>();

            CreateMap<CreateCustomerDto, Customer>();
            CreateMap<UpdateCustomerDto, Customer>();

            CreateMap<Customer, CustomerWithOrdersDto>()
                .ForMember(d => d.Orders, opt => opt.MapFrom(s => s.Orders));

            // ---------------- Orders + Items ----------------

            CreateMap<Order, OrderDto>()
                .ForMember(d => d.CustomerName,
                    opt => opt.MapFrom(s => s.Customer.FirstName + " " + s.Customer.LastName));

            CreateMap<CreateOrderDto, Order>();
            CreateMap<UpdateOrderDto, Order>();

            CreateMap<Order, OrderWithItemsDto>()
                .ForMember(d => d.CustomerName,
                    opt => opt.MapFrom(s => s.Customer.FirstName + " " + s.Customer.LastName))
                .ForMember(d => d.Items,
                    opt => opt.MapFrom(s => s.OrderItems));   // Order.OrderItems -> Items

            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(d => d.ProductName,
                    opt => opt.MapFrom(s => s.Product.ProductName));

            // ---------------- Suppliers ----------------

            CreateMap<Supplier, SupplierDto>();

            CreateMap<CreateSupplierDto, Supplier>();
            CreateMap<UpdateSupplierDto, Supplier>();

            // ---------------- Products ----------------

            CreateMap<Product, ProductDto>()
                .ForMember(d => d.SupplierName,
                    opt => opt.MapFrom(s => s.Supplier.CompanyName));

            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
        }
    }
}
