-- ============================================================================
-- 002 — Security audit helper (OPTIONAL, review before running)
--
-- Context: before v2, initial admin passwords were generated deterministically
-- from the Staff_ID (SHA256(Staff_ID) first 12 hex chars, uppercase). Any
-- A1/B2 account that never changed its password is compromised by design.
--
-- This script only LISTS the accounts that should be forced to reset their
-- password. Trigger resets through the normal /api/PasswordReset flow.
-- ============================================================================

SELECT Staff_ID, Name, Email, Lab_role, ApprovalDate
FROM dbo.[User]
WHERE Lab_role IN ('A1', 'B2')
ORDER BY Lab_role, Name;

-- Also export the stored procedures the API still depends on, so they can be
-- checked into source control (run and save the output):
--
-- SELECT ROUTINE_NAME, ROUTINE_DEFINITION
-- FROM INFORMATION_SCHEMA.ROUTINES
-- WHERE ROUTINE_TYPE = 'PROCEDURE'
--   AND ROUTINE_NAME IN ('InsertLeaveRequest', 'GetLeaveRequests', 'ApproveLeaveRequest',
--                        'InsertImage', 'GetImageById');
