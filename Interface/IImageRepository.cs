using Employee_History.Models;

namespace Employee_History.Interface
{
    public interface IImageRepository
    {
       public Task<int> InsertImageAsync(ImageModel image, string staffId);
       public Task<byte[]> GetImageAsync(string staffId);
    }
}
