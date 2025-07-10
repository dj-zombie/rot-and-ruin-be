using AutoMapper;
using RotAndRuin.Application.DTOs;
using RotAndRuin.Domain.Entities;
using RotAndRuin.Domain.Common;
using RotAndRuin.Application.DTOs.Requests;

namespace RotAndRuin.Application.Mapping;

public class MappingProfile : Profile
{
    private static string GetFeaturedImageThumbnailUrl(Product product)
    {
        var featuredImage = product.ProductImages?.FirstOrDefault(x => x.IsFeatured);
        return featuredImage?.GridThumbnailUrl ?? string.Empty;
    }
    public MappingProfile()
    {
        // Base mapping for Product
        CreateMap<Product, ProductDto>()
            .ReverseMap();

        // Base mapping for ProductImage
        CreateMap<ProductImage, ProductImageDto>()
            .ReverseMap()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore());

        // Grid DTO mapping
        CreateMap<Product, ProductGridDto>()
            .ForMember(dest => dest.GridThumbnailUrl, opt => opt.MapFrom(src => GetFeaturedImageThumbnailUrl(src)));

        // Product creation mapping
        CreateMap<ProductCreateRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}