using System.ComponentModel.DataAnnotations;

namespace Employee_History.Features.Users
{
    // ------------------------------------------------------------------
    // Requests
    // ------------------------------------------------------------------

    /// <summary>Body for creating a user. A1/B2 users get a generated initial password emailed to them.</summary>
    public class AddUserRequest
    {
        [Required]
        public string Staff_ID { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public long Phone_number { get; set; }

        /// <summary>A1 = super admin, B2 = sub admin, C3 = staff.</summary>
        [Required, RegularExpression("^(A1|B2|C3)$", ErrorMessage = "Lab_role must be A1, B2 or C3.")]
        public string Lab_role { get; set; } = string.Empty;
    }

    /// <summary>Body for actions that target one user by staff id (approve, deny, remove, reset device).</summary>
    public class StaffIdRequest
    {
        [Required]
        public string Staff_ID { get; set; } = string.Empty;
    }

    // ------------------------------------------------------------------
    // Responses  (camelCase on the wire: staff_ID, lab_role, ...)
    // ------------------------------------------------------------------

    /// <summary>Public user shape. Never includes the password hash or device binding.</summary>
    public class UserDto
    {
        public string Staff_ID { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Email { get; set; }
        public long? Phone_number { get; set; }
        public string? Lab_role { get; set; }
        public bool ApprovalStatus { get; set; }
        public DateTime? ApprovalDate { get; set; }
    }

    /// <summary>Approval-history row. Id (= staff id) is the stable key for deletes.</summary>
    public class ApprovalRecordDto
    {
        public string Id { get; set; } = string.Empty;
        public string Staff_ID { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool ApprovalStatus { get; set; }
        public DateTime? Date { get; set; }
    }

    /// <summary>Removal-history row. Id (= staff id) is the stable key for deletes.</summary>
    public class RemovalRecordDto
    {
        public string Id { get; set; } = string.Empty;
        public string Staff_ID { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Email { get; set; }
        public DateTime? Date { get; set; }
    }

    /// <summary>Response for AddUser: <c>{ success, message, staff_ID }</c>.</summary>
    public class AddUserResponse : Common.Models.ApiMessage
    {
        public string? Staff_ID { get; set; }
    }

    /// <summary>Thrown when adding a user whose Staff_ID already exists (maps to HTTP 409).</summary>
    public class DuplicateStaffIdException : Exception
    {
        public DuplicateStaffIdException(string staffId)
            : base($"Staff ID '{staffId}' already exists.") { }
    }
}
