using Employee_History.DappaRepo;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Employee_History.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : Controller
    {
        private readonly IDappaEmployee dappaEmployee;
        public AttendanceController(IDappaEmployee dappaEmployee)
        {
            this.dappaEmployee = dappaEmployee;
        }
        [HttpGet("Attendance History")]
        public async Task<IEnumerable<Attendance_History>> GetAttendance() 
        {
            try
            {
                return await dappaEmployee.GetAttendance();
            }
            catch
            {
                throw;
            }
        }

        [HttpGet("AttendanceByID")]
        public async Task<Attendance_History> GetAttendanceByID(string StaffID)
        {
            try
            {
                var response = await dappaEmployee.GetAttendanceByID(StaffID);
                if (response == null)
                {
                    return null;
                }
                return response;
            }
            catch
            {
                throw;
            }
        }

        [HttpGet("Attendance By Date ")]
        public async Task<IEnumerable<Attendance_History>> GetAttendanceByDate(DateTime Date)
        {
            try
            {
                var response = await dappaEmployee.GetAttendanceByDate(Date);
                if (response == null)
                {
                    return null;
                }
                return response;
            }
            catch
            {
                throw;
            }
        }

        [HttpGet(" GetAttendanceByIDandDate")]
        public async Task<Attendance_History> GetAttendanceByIDandDate(string StaffID, DateTime Date)
        {
            try
            {
                var response = await dappaEmployee.GetAttendanceByIDandDate(StaffID,Date);
                if (response == null)
                {
                    return null;
                }
                return response;
            }
            catch
            {
                throw;
            }
        }

        [HttpGet("GetAttendanceByIDbtwDates")]
        public async Task<IEnumerable<Attendance_History>> GetAttendanceByIDbtwDates(string StaffID, DateTime startDate, DateTime endDate)
        {
            try
            {
                var response = await dappaEmployee.GetAttendanceByIDbtwDates(StaffID, startDate,endDate);
                if (response == null)
                {
                    return null;
                }
                return response;
            }
            catch
            {
                throw;
            }
        }

        [HttpGet("GetAttendancebtwDates")]
        public async Task<IEnumerable<Attendance_History>> GetAttendancebtwDates( DateTime startDate, DateTime endDate)
        {
            try
            {
                var response = await dappaEmployee.GetAttendancebtwDates( startDate, endDate);
                if (response == null)
                {
                    return null;
                }
                return response;
            }
            catch
            {
                throw;
            }
        }







        /*  public IActionResult Index()
          {
              return View();
          }*/
    }
}
