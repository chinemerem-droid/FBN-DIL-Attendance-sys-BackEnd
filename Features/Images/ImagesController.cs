using Employee_History.Common;
using Employee_History.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Images
{
    /// <summary>
    /// Profile images: upload (multipart) and fetch. Staff can only manage
    /// their own image; admins (A1/B2) can manage anyone's. JPEG/PNG only,
    /// max 5 MB, one image per staff member (re-upload replaces).
    /// </summary>
    [Route("api/Image")]
    [Authorize]
    public class ImagesController : ApiControllerBase
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png" };

        private readonly IImageRepository _images;

        public ImagesController(IImageRepository images)
        {
            _images = images;
        }

        /// <summary>Uploads (or replaces) a profile image and returns the stored image.</summary>
        /// <remarks>Expects: multipart/form-data with "file" (JPEG/PNG, max 5 MB) and "staff_ID" fields; own id unless admin. Returns: 200 with the image bytes; 400 on invalid file; 403 for someone else's image.</remarks>
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] string staff_ID)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ApiMessage("Invalid file.", false));
            }
            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest(new ApiMessage("File exceeds the 5 MB limit.", false));
            }
            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                return BadRequest(new ApiMessage("Only JPEG and PNG images are allowed.", false));
            }
            if (string.IsNullOrEmpty(staff_ID))
            {
                return BadRequest(new ApiMessage("Staff ID is required.", false));
            }
            if (!CallerIsAdmin && staff_ID != CallerStaffId)
            {
                return Forbid();
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var image = new ImageModel
            {
                FileName = file.FileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                ImageData = memoryStream.ToArray(),
                Staff_ID = staff_ID
            };
            await _images.InsertImageAsync(image, staff_ID);

            var imageData = await _images.GetImageAsync(staff_ID);
            if (imageData == null)
            {
                return NotFound(new ApiMessage("Image not found after upload.", false));
            }
            return File(imageData, file.ContentType);
        }

        /// <summary>Fetches a staff member's profile image.</summary>
        /// <remarks>Expects: staff id in the URL; own id unless admin. Returns: 200 with the image bytes; 404 when none exists.</remarks>
        [HttpGet("{staffId}")]
        public async Task<IActionResult> GetImageByStaffId(string staffId)
        {
            if (!CallerIsAdmin && staffId != CallerStaffId)
            {
                return Forbid();
            }

            var imageData = await _images.GetImageAsync(staffId);
            if (imageData == null)
            {
                return NotFound(new ApiMessage("Image not found.", false));
            }
            return File(imageData, "image/jpeg");
        }

        /// <summary>Legacy image fetch (POST body instead of URL).</summary>
        /// <remarks>Expects: { staff_ID }; own id unless admin. Returns: 200 with the image bytes; 404 when none exists.</remarks>
        [HttpPost]
        public async Task<IActionResult> GetImage([FromBody] ImageLookupRequest request)
        {
            if (!CallerIsAdmin && request.Staff_ID != CallerStaffId)
            {
                return Forbid();
            }

            var imageData = await _images.GetImageAsync(request.Staff_ID);
            if (imageData == null)
            {
                return NotFound(new ApiMessage("Image not found.", false));
            }
            return File(imageData, "image/jpeg");
        }
    }
}
