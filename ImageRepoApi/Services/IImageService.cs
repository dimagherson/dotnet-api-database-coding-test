using ImageConverterApi.Models;
using ImageRepoApi.Services;

namespace ImageConverterApi.Services
{
    public interface IImageService
    {
        Task<ImportImageResult> ImportImage(ImageUploadModel model, Stream imageData, string fileName);
    }
}