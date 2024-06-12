using System.ComponentModel.DataAnnotations;

namespace Employee_History.Models
{
    public class ImageModel
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; } = long.MinValue;
        public byte[] ImageData { get; set; } = new byte[0];
        [Key]
        public string Staff_ID { get; set; }
    }

}
