using Employee_History.Models;

namespace Employee_History.DappaRepo
{
    public interface IImageRepository
    {
        Task<int> InsertImageAsync(ImageModel image,string StaffID);
    }
}
