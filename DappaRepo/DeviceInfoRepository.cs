using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Employee_History.Interface;
using Employee_History.Models;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using Microsoft.Data.SqlClient;



namespace Employee_History.DappaRepo
{
    public class DeviceInfoRepository : IDeviceInfoRepository
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection _connection;

        public DeviceInfoRepository(IConfiguration configuration)
        {

            _configuration = configuration;
            _connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<int> AddDeviceInfoAsync(DeviceInfoModel deviceInfo)
        {
           
                await _connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@DeviceId", deviceInfo.DeviceId);
                parameters.Add("@Staff_ID", deviceInfo.Staff_ID);
                parameters.Add("@DeviceInfo", deviceInfo.DeviceInfo);
                return await _connection.ExecuteAsync("AddDeviceInfo", parameters, commandType: CommandType.StoredProcedure);
            
        }
    }
}
