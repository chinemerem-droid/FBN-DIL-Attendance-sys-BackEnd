using Employee_History.Models;

namespace Employee_History.DappaRepo
{
    public interface IDappaEmployee
    {
        public Task<IEnumerable<Attendance_History>> GetAttendance();
        public Task<Attendance_History> GetAttendanceByID(string StaffID);

        public Task<IEnumerable<Attendance_History>> GetAttendanceByDate(DateTime Date);

        public Task<Attendance_History> GetAttendanceByIDandDate(string StaffID, DateTime Date);

        public Task<IEnumerable<Attendance_History>> GetAttendanceByIDbtwDates(string StaffID, DateTime startDate, DateTime endDate);

        public Task<IEnumerable<Attendance_History>> GetAttendancebtwDates(DateTime startDate, DateTime endDate);


    }
}
