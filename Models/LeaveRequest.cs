namespace Employee_History.Models
{
    public class LeaveRequest
    {
        public string Staff_ID { get; set; }
        public DateTime StartDate {  get; set; }
        public DateTime EndDate { get; set; }

    }
}
