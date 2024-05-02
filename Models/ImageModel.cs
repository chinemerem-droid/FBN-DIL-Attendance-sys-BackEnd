using System.ComponentModel.DataAnnotations;

namespace Employee_History.Models
{
    public class ImageModel
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public byte[] ImageData { get; set; }
        [Key]
        public string? Staff_ID { get; set; }
    }

}
