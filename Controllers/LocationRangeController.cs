using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class LocationRangeController : Controller
    {


        private readonly IConfiguration _configuration;

        public LocationRangeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationRange()
        {
            decimal minLongitude = _configuration.GetValue<decimal>("LocationRange:MinLongitude", 0m);
            decimal maxLongitude = _configuration.GetValue<decimal>("LocationRange:MaxLongitude", 0m);
            decimal minLatitude = _configuration.GetValue<decimal>("LocationRange:MinLatitude", 0m);
            decimal maxLatitude = _configuration.GetValue<decimal>("LocationRange:MaxLatitude", 0m);

            var locationRange = new LocationRange
            {
                MinLongitude = minLongitude,
                MaxLongitude = maxLongitude,
                MinLatitude = minLatitude,
                MaxLatitude = maxLatitude
            };

            return Ok(locationRange);
        }

    }
}
