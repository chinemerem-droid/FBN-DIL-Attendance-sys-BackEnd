using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_History.Models
{
    public class Attendance_History
    {
        [Key]
        public string Staff_ID { get; set; } = string.Empty;
        public TimeSpan EntryTime { get; set; }
        public TimeSpan ExitTime { get; set; }
        public DateTime Date { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CheckinStatus { get; set; } = string.Empty;

        [NotMapped]
        public string EntryTimeString => EntryTime.ToString(@"hh\:mm\:ss");

        [NotMapped]
        public string ExitTimeString => ExitTime.ToString(@"hh\:mm\:ss");
    }
}
