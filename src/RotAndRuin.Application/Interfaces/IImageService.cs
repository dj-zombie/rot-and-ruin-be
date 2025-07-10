using RotAndRuin.Domain.Entities;

namespace RotAndRuin.Application.Interfaces;

public interface IImageService
{
    Task<ProductImage> ProcessAndUploadProductImageAsync(Stream imageStream, string fileName, bool isFeatured);
}