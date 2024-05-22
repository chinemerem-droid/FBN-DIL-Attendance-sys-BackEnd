namespace Employee_History.Models
{
    public class User
    {

        public string Staff_ID { get; set; }
        public string? Name { get; set; }

        public string Password { get; set; } = string.Empty;

        public string? Email { get; set; }

        public long Phone_number { get; set; }

        public string? Lab_role { get; set; }

        public bool ApprovalStatus { get; set; }
        public string DeviceID { get; set; }
        public string DeviceModel { get; set; }

        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }



    }
}
