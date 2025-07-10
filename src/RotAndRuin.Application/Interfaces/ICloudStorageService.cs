namespace RotAndRuin.Application.Interfaces;

public interface ICloudStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
}