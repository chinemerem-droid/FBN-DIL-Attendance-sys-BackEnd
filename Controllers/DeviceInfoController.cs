using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Employee_History.Interface;
using Employee_History.Models;

namespace Employee_History.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceInfoController : ControllerBase
    {
        private readonly IDeviceInfoRepository _deviceInfoRepository;

        public DeviceInfoController(IDeviceInfoRepository deviceInfoRepository)
        {
            _deviceInfoRepository = deviceInfoRepository;
        }

        [HttpPost("AddDeviceInfo")]
        public async Task<IActionResult> AddDeviceInfo(DeviceInfoModel deviceInfo)
        {

            int resultCode = await _deviceInfoRepository.StoreDeviceInfo(deviceInfo);

            if (resultCode == -1)
            {
                return Ok("Device information added/updated successfully.");
            }
            else if (resultCode == 0)
            {
                return BadRequest("Device ID mismatch. Access denied.");
            }
            else
            {
                return StatusCode(500);
            }
        }


    }
}
