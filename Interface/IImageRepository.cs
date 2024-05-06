using Employee_History.Models;

namespace Employee_History.Interface
{
    public interface IImageRepository
    {
       public Task<int> InsertImageAsync(ImageModel image, string Staff_ID);
       public Task<byte[]> GetImageAsync(string staff_ID);
    }
}
