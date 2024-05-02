using Employee_History.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;

namespace Employee_History.DappaRepo
{
    public class ImageRepository:IImageRepository
    {
        private readonly string _connectionString;

        public ImageRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> InsertImageAsync(ImageModel image,string Staff_ID)
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
                    Staff_ID =image.Staff_ID,
                };
                return await connection.ExecuteAsync("InsertImage", parameters, commandType: CommandType.StoredProcedure);
            }
        }
    }

}
