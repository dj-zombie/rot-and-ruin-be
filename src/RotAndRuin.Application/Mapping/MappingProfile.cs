using AutoMapper;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Domain.Entities;
using RotAndRuin.Application.DTOs.Requests;
using RotAndRuin.Domain.Entities.Cart;
using RotAndRuin.Domain.Entities.Order;

namespace RotAndRuin.Application.Mapping;

public class MappingProfile : Profile
{
    private static string GetFeaturedImageThumbnailUrl(Product product)
    {
        var featuredImage = product.ProductImages?.FirstOrDefault(x => x.IsFeatured);
        return featuredImage?.OriginalUrl ?? string.Empty;
    }

    private static decimal CalculateSubTotal(CartItem item)
    {
        return item.Product.Price * item.Quantity;
    }

    private static decimal CalculateShippingTotal(CartItem item)
    {
        return item.Product.ShippingPrice * item.Quantity;
    }

    public MappingProfile()
    {
        // Product mappings
        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<ProductImage, ProductImageDto>().ReverseMap()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore());
        CreateMap<Product, ProductGridDto>()
            .ForMember(dest => dest.OriginalUrl, 
                opt => opt.MapFrom(src => GetFeaturedImageThumbnailUrl(src)));
        CreateMap<ProductCreateRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // Cart mappings
        CreateMap<Cart, CartDto>()
            .ForMember(d => d.PaymentIntentId, o => o.MapFrom(s => s.PaymentIntentId))
            .ForMember(d => d.ClientSecret, o => o.MapFrom(s => s.ClientSecret))
            .ForMember(dest => dest.SubTotal, 
                opt => opt.MapFrom(src => src.Items.Sum(i => CalculateSubTotal(i))))
            .ForMember(dest => dest.ShippingTotal, 
                opt => opt.MapFrom(src => src.Items.Sum(i => CalculateShippingTotal(i))))
            .ForMember(dest => dest.Total, 
                opt => opt.MapFrom(src => 
                    src.Items.Sum(i => CalculateSubTotal(i) + CalculateShippingTotal(i))));

        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.ProductName, 
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductUrl, 
                opt => opt.MapFrom(src => src.Product.ProductUrl))
            .ForMember(dest => dest.OriginalUrl, 
                opt => opt.MapFrom(src => GetFeaturedImageThumbnailUrl(src.Product)))
            .ForMember(dest => dest.Price, 
                opt => opt.MapFrom(src => src.Product.Price))
            .ForMember(dest => dest.ShippingPrice, 
                opt => opt.MapFrom(src => src.Product.ShippingPrice))
            .ForMember(dest => dest.SubTotal, 
                opt => opt.MapFrom(src => CalculateSubTotal(src)))
            .ForMember(dest => dest.ShippingTotal, 
                opt => opt.MapFrom(src => CalculateShippingTotal(src)))
            .ForMember(dest => dest.Total, 
                opt => opt.MapFrom(src => CalculateSubTotal(src) + CalculateShippingTotal(src)));

        // Order mappings
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId));
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName, 
                opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductUrl, 
                opt => opt.MapFrom(src => src.Product.ProductUrl))
            .ForMember(dest => dest.OriginalUrl, 
                opt => opt.MapFrom(src => GetFeaturedImageThumbnailUrl(src.Product)));

        CreateMap<ShippingAddress, ShippingAddressDto>().ReverseMap();
        CreateMap<CreateOrderRequest, ShippingAddress>();
    }
}