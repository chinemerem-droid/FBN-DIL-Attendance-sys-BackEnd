using System.Threading.Tasks;
using Employee_History.Models;

namespace Employee_History.Interface
{
    public interface IDeviceInfoRepository
    {
        Task<int> AddDeviceInfoAsync(DeviceInfoModel deviceInfo);
    }
}
