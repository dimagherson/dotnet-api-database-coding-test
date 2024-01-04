using System.Text.RegularExpressions;
using ImageConverterApi.Models;
using ImageConverterApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Storage;

namespace ImageConverterApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly IImageService _imageService;
        private readonly DatabaseContext _dbContext;

        public ImageController(ILogger<ImageController> logger, IImageService imageService, DatabaseContext dbContext)
        {
            _logger = logger;
            _imageService = imageService;
            _dbContext = dbContext;
        }


        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] ImageUploadModel model, IFormFile imageFile)
        {
            // Validate input
            if (model.KeepAspectRatio && model.TargetWidth <= 0 && model.TargetHeight <= 0)
            {
                return BadRequest("Invalid target dimensions");
            }

            if (!model.KeepAspectRatio && (model.TargetWidth <= 0 || model.TargetHeight <= 0))
            {
                return BadRequest("Invalid target dimensions");
            }
            
            if (string.IsNullOrEmpty(model.TargetFormat) || !Regex.IsMatch(model.TargetFormat, "png|jpeg", RegexOptions.IgnoreCase))
                return BadRequest("Invalid target format");

            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No image file uploaded");


            // Import the image
            using var fileStream = imageFile.OpenReadStream();
            var imageId = await _imageService.ImportImage(model, fileStream, imageFile.FileName);

            // Log the upload
            _logger.LogInformation($"Uploaded image {imageId} with format {model.TargetFormat} and dimensions {model.TargetWidth}x{model.TargetHeight}");

            // Return the image ID
            return Ok(new { imageId = imageId.ToString() });
        }


        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            // Lookup the image by ID
            var image = await _dbContext.Images.FirstOrDefaultAsync(i => i.ImageId == id);

            // If not found (or invalid) return 404
            if (image == null || image.Data == null)
                return NotFound();

            // Return the image with the correct content type
            return File(image.Data, $"image/{image.ImageFormat}");
        }

        [HttpGet]
        public async Task<IActionResult> Info(Guid id)
        {
            var image = await _dbContext.Images.FirstOrDefaultAsync(i => i.ImageId == id);

            if (image == null || image.Data == null)
            {
                return NotFound();
            }

            return Ok(new {
                image.FileName,
                format = image.ImageFormat,
                createdAtUtc = image.CreatedAt.ToUniversalTime(),
                image.Width,
                image.Height,
                size = image.Data.Length
            });
        }
    }
}
