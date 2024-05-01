﻿using Employee_History.DappaRepo;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ImageRepository _imageRepository;

        public ImageController(ImageRepository imageRepository)
        {
            _imageRepository = imageRepository;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, string StaffID)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Invalid file");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var image = new ImageModel
            {
                FileName = file.FileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                ImageData = memoryStream.ToArray(),
                 StaffID=StaffID.ToString()
            };

            // Pass the StaffID argument obtained from the request or elsewhere
            await _imageRepository.InsertImageAsync(image, StaffID);

            return Ok("Image uploaded successfully");
        }
    }
}