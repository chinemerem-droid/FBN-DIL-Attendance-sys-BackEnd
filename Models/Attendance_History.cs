using System.ComponentModel.DataAnnotations;

namespace Employee_History.Models
{
    public class Attendance_History
    {
        [Key]
        public string StaffID { get; set; }
        public TimeSpan EntryTime { get; set; }
        public TimeSpan ExitTime { get; set; }
        public DateTime Date { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
