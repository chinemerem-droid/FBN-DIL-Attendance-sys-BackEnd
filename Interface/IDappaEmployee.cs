using Employee_History.Models;

namespace Employee_History.Interface
{
    public interface IDappaEmployee
    {
        public Task<IEnumerable<Attendance_History>> GetAttendance();
        public Task<Attendance_History> GetAttendanceByID(string Staff_ID);

        public Task<IEnumerable<Attendance_History>> GetAttendanceByDate(DateTime Date);

        public Task<Attendance_History> GetAttendanceByIDandDate(string Staff_ID, DateTime Date);

        public Task<IEnumerable<Attendance_History>> GetAttendanceByIDbtwDates(string Staff_ID, DateTime startDate, DateTime endDate);

        public Task<IEnumerable<Attendance_History>> GetAttendancebtwDates(DateTime startDate, DateTime endDate);

        public Task<Attendance_History> Checkin(string Staff_ID);

        public Task<Attendance_History> Checkout(string staff_ID);
        public Task<IEnumerable<Attendance_History>> GetLateCheckinStaffAsync();

    }
}
