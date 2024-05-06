using Employee_History.DappaRepo;
using Employee_History.Models;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Employee_History.Interface;

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
        public async Task<Attendance_History> GetAttendanceByID(string Staff_ID)
        {
            try
            {
                var response = await dappaEmployee.GetAttendanceByID(Staff_ID);
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
        public async Task<Attendance_History> GetAttendanceByIDandDate(string Staff_ID, DateTime Date)
        {
            try
            {
                var response = await dappaEmployee.GetAttendanceByIDandDate(Staff_ID, Date);
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
        public async Task<IEnumerable<Attendance_History>> GetAttendanceByIDbtwDates(string Staff_ID, DateTime startDate, DateTime endDate)
        {
            try
            {
                var response = await dappaEmployee.GetAttendanceByIDbtwDates(Staff_ID, startDate,endDate);
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

        [HttpPost]
        public async Task<IActionResult> Checkin(string Staff_ID)
        {
            try
            {
                // Call the repository method to add the user
                var user = await dappaEmployee.Checkin(Staff_ID);
                return Ok("User Checked in");


            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);

                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpPut]
        public async Task<IActionResult> Checkout(string Staff_ID)
        {
            try
            {
                // Call the repository method to update the exit time
                await dappaEmployee.Checkout(Staff_ID);

                // Return a success message
                return Ok("Exit time updated successfully");
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex);

                // Return a server error message
                return StatusCode(500, "An error occurred while updating exit time");
            }
        }

        [HttpGet("Late checkin")]
        public async Task<IEnumerable<Attendance_History>> GetLateCheckinStaffAsync()
        {
            try
            {
                return await dappaEmployee.GetLateCheckinStaffAsync();
            }
            catch
            {
                throw;
            }
        }


    }
}
