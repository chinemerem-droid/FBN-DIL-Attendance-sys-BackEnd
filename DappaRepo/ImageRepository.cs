using Employee_History.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;
using Employee_History.Interface;

namespace Employee_History.DappaRepo
{
    public class ImageRepository : IImageRepository
    {
        private readonly string _connectionString;

        public ImageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public virtual async Task<int> InsertImageAsync(ImageModel image, string staffId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new
                {
                    FileName = image.FileName,
                    FileType = image.FileType,
                    FileSize = image.FileSize,
                    ImageData = image.ImageData,
                    Staff_ID = staffId,
                };
                return await connection.ExecuteAsync("InsertImage", parameters, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<byte[]> GetImageAsync(string staffId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@Staff_ID", staffId);
                var imageData = await connection.QueryFirstOrDefaultAsync<byte[]>("GetImageById", parameters, commandType: CommandType.StoredProcedure);
                return imageData;
            }
        }

    }


}
