namespace Employee_History.Models
{
    public class User
    {

        public string Staff_ID { get; set; }
        public string? Name { get; set; }  = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? Email { get; set; } = string.Empty;

        public long Phone_number { get; set; }= long.MinValue;

        public string? Lab_role { get; set; } = string.Empty;

        public bool ApprovalStatus { get; set; } = false;
        public DateTime ApprovalDate {  get; set; }= DateTime.MinValue;
        public DateTime RemovalDate {  get; set; }= DateTime.MinValue;
        public string DeviceID { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;

        public decimal Longitude { get; set; }= decimal.MinValue;
        public decimal Latitude { get; set; }=decimal.MinValue;



    }
}
