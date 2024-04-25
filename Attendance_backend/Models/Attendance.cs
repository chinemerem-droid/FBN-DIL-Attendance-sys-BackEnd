namespace Attendance_backend.Models
{
    public class Attendance
    {
        public string UserID { get; set; }
        public string StaffID { get; set; }
        public string EntryTime { get; set; }
        public string ExitTime { get; set; }
        public string Date { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string Suite { get; set; }
    }
}
