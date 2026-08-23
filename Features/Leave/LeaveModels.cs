using System.ComponentModel.DataAnnotations;

namespace Employee_History.Features.Leave
{
    /// <summary>One leave request row: <c>{ id, staff_ID, startDate, endDate, status }</c>.</summary>
    public class LeaveRequestDto
    {
        public int Id { get; set; }
        public string Staff_ID { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>Body for submitting a leave request. Staff omit staff_ID (their own is used).</summary>
    public class CreateLeaveRequest
    {
        public string? Staff_ID { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}
