namespace Employee_History.Features.Users
{
    /// <summary>
    /// Database entity for the [User] table. Never returned from the API
    /// directly — use <see cref="UserDto"/> for responses.
    /// </summary>
    public class User
    {
        public string Staff_ID { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Email { get; set; }
        public long Phone_number { get; set; }
        public string? Lab_role { get; set; }
        public string? Password { get; set; }
        public bool ApprovalStatus { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? RemovalDate { get; set; }
        public string? DeviceID { get; set; }
        public string? DeviceModel { get; set; }
    }
}
