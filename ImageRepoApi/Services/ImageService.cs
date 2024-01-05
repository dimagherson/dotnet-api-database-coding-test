using System.Numerics;
using ImageConverterApi.Models;
using ImageRepoApi.Services;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Storage;
using Storage.Entities;

namespace ImageConverterApi.Services
{
    public class ImageService : IImageService
    {
        private readonly DatabaseContext _dbContext;

        public ImageService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ImportImageResult> ImportImage(ImageUploadModel model, Stream imageData, string fileName)
        {
            if (!Enum.TryParse<SKEncodedImageFormat>(model.TargetFormat, true, out var format))
                throw new ArgumentException($"Invalid image format: {model.TargetFormat}");
            
            var resizedImage = ResizeImage(imageData, format, model.TargetWidth, model.TargetHeight, model.KeepAspectRatio);
            var image = new Image
            {
                CreatedAt = DateTime.UtcNow,
                Data = resizedImage,
                FileName = fileName,
                Width = model.TargetWidth,
                Height = model.TargetHeight,
                ImageFormat = format.ToString().ToLower(),
                Hash = new BigInteger(resizedImage).GetHashCode()
            };

            var imagesByHash = await _dbContext.Images.Where(i => i.Hash == image.Hash).ToListAsync();
            var matchingImage = imagesByHash.FirstOrDefault(i => image.Data.SequenceEqual(i.Data!));

            if (matchingImage != null)
            {
                return new ImportImageResult(matchingImage.ImageId, true);
            }

            _dbContext.Images.Add(image);
            await _dbContext.SaveChangesAsync();

            return new ImportImageResult(image.ImageId, false);
        }


        private byte[] ResizeImage(Stream sourceImage, SKEncodedImageFormat newFormat, int newWidth, int newHeight, bool keepAspectRatio)
        {
            using var img = SKImage.FromEncodedData(sourceImage);
            using var resizedImg = ResizeImage(img, newWidth, newHeight, keepAspectRatio);
            using var data = resizedImg.Encode(newFormat, 100);
            
            return data.ToArray();
        }

        private SKImage ResizeImage(SKImage sourceImage, int newWidth, int newHeight, bool keepAspectRatio)
        {
            if (keepAspectRatio)
            {
                (newWidth, newHeight) = DetermineAspectRatioSize(newWidth, newHeight, sourceImage.Width, sourceImage.Height);
            }

            var destRect = new SKRect(0, 0, newWidth, newHeight);
            var srcRect = new SKRect(0, 0, sourceImage.Width, sourceImage.Height);

            using var result = new SKBitmap((int)destRect.Width, (int)destRect.Height);
            using var g = new SKCanvas(result);
            using var p = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            g.DrawImage(sourceImage, srcRect, destRect, p);

            return SKImage.FromBitmap(result);
        }

        private (int newWidth, int newHeight) DetermineAspectRatioSize(int targetWidth, int targetHeight, int currentWidth, int currentHeight)
        {
            var ratio = currentHeight / (double)currentWidth;

            if (targetWidth > 0)
            {
                targetHeight = (int)(targetWidth * ratio);
            }
            else
            {
                targetWidth = (int)(targetHeight / ratio);
            }

            return (targetWidth, targetHeight);
        }
    }
}
