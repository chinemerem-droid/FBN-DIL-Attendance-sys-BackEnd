using Employee_History.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_History.Features.Location
{
    /// <summary>The office geofence bounding box used to validate mobile check-ins.</summary>
    public class LocationRange
    {
        public decimal MinLongitude { get; set; }
        public decimal MaxLongitude { get; set; }
        public decimal MinLatitude { get; set; }
        public decimal MaxLatitude { get; set; }
    }

    /// <summary>
    /// Exposes the configured office geofence so the mobile app can
    /// pre-validate the user's position before attempting a check-in.
    /// </summary>
    [Route("api/LocationRange")]
    [Authorize]
    public class LocationController : ApiControllerBase
    {
        private readonly IConfiguration _configuration;

        public LocationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>Returns the office geofence bounding box.</summary>
        /// <remarks>Expects: bearer token only. Returns: 200 { minLongitude, maxLongitude, minLatitude, maxLatitude }.</remarks>
        [HttpGet]
        public IActionResult GetLocationRange()
        {
            var locationRange = new LocationRange
            {
                MinLongitude = _configuration.GetValue<decimal>("LocationRange:MinLongitude", 0m),
                MaxLongitude = _configuration.GetValue<decimal>("LocationRange:MaxLongitude", 0m),
                MinLatitude = _configuration.GetValue<decimal>("LocationRange:MinLatitude", 0m),
                MaxLatitude = _configuration.GetValue<decimal>("LocationRange:MaxLatitude", 0m)
            };

            return Ok(locationRange);
        }
    }
}
