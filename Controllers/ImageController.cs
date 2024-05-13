using Employee_History.DappaRepo;
using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;

namespace Employee_History.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageRepository _imageRepository;

        public ImageController(IImageRepository imageRepository)
        {
            _imageRepository = imageRepository;
        }


        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadAndGetImage(IFormFile file, string Staff_ID)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Invalid file");
            }

            // Upload image
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var image = new ImageModel
            {
                FileName = file.FileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                ImageData = memoryStream.ToArray(),
                Staff_ID = Staff_ID.ToString()
            };
            await _imageRepository.InsertImageAsync(image, Staff_ID);
          
            var imageData = await _imageRepository.GetImageAsync(Staff_ID);

            if (imageData == null)
            {
                return NotFound(); // Return 404 if image data not found
            }

            // Return image data
            return File(imageData, "image/jpeg"); // Assuming the image is JPEG format, change MIME type accordingly
        }

        [HttpGet("image/{staffId}")]
        public async Task<IActionResult> GetImage(string staffId)
        {
            var imageData = await _imageRepository.GetImageAsync(staffId);

            if (imageData == null)
            {
                return NotFound(); // Return 404 if image data not found
            }

            // Return image data
            return File(imageData, "image/jpeg"); // Assuming the image is JPEG format, change MIME type accordingly
        }

    }
}
