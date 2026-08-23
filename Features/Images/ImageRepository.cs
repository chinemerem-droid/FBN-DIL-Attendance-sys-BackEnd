using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Employee_History.Features.Images
{
    /// <summary>A stored profile image (Images table, one per staff member).</summary>
    public class ImageModel
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string Staff_ID { get; set; } = string.Empty;
    }

    /// <summary>Body for fetching an image by staff id (legacy POST route).</summary>
    public class ImageLookupRequest
    {
        public string Staff_ID { get; set; } = string.Empty;
    }

    /// <summary>Data access for profile images (stored procedures InsertImage / GetImageById).</summary>
    public interface IImageRepository
    {
        Task<int> InsertImageAsync(ImageModel image, string staffId);
        Task<byte[]?> GetImageAsync(string staffId);
    }

    public class ImageRepository : IImageRepository
    {
        private readonly SqlConnection _connection;

        public ImageRepository(SqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<int> InsertImageAsync(ImageModel image, string staffId)
        {
            var parameters = new
            {
                image.FileName,
                image.FileType,
                image.FileSize,
                image.ImageData,
                Staff_ID = staffId,
            };
            return await _connection.ExecuteAsync("InsertImage", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<byte[]?> GetImageAsync(string staffId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Staff_ID", staffId);
            return await _connection.QueryFirstOrDefaultAsync<byte[]?>("GetImageById", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
