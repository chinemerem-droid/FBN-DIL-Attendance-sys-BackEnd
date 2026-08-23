# FBN-DIL Attendance System — Backend

ASP.NET Core API (**.NET 10 LTS**) for the AMS attendance system (admin web portal + mobile check-in).

## Project structure (feature-based)

```
Common/                  Cross-cutting building blocks
  ApiControllerBase.cs     Caller identity helpers (staff id, role) for all controllers
  Models/                  ApiMessage + PagedResult envelopes
  Middleware/              Global exception handler (JSON errors, no stack traces)
  Security/                PasswordHasher (PBKDF2), TokenGenerator (JWT + secure tokens), RefreshTokenStore
  Health/                  SQL health check
Features/                One folder per feature: controller + repository + models together
  Auth/                    Logins (admin + device-bound mobile), refresh/logout, confirm/change password
  Users/                   User lifecycle: add, approve/deny, remove, lists, history, me, reset device
  Notifications/           Role-addressed admin notifications: list, unread count, mark read
  Attendance/              Check-in/out capture + history, by-date, ranges, late list, daily summary
  Leave/                   Leave requests + approval
  PasswordReset/           Email token flow: request, verify, reset
  Images/                  Profile images (multipart upload + fetch)
  Email/                   SMTP service + super-admin send endpoint
  Location/                Office geofence for the mobile app
Database/                Schema, idempotent migrations, LocalDB setup script
Program.cs               Composition root: config, auth policies, rate limiting, DI, Swagger
```

Every controller and action carries XML `<summary>` docs (what it does, what it
expects, what it returns) — these render in Swagger at `/swagger`. All request
bodies are slim, validated DTOs; validation failures return the standard
`{ success, message }` envelope with HTTP 400.

## Setup

### 1. Configuration / secrets

Secrets are **not** stored in `appsettings.json`. They are read from (highest wins):

1. Environment variables (production — Render/Docker)
2. `appsettings.Local.json` (local development — git-ignored)
3. `appsettings.json` (non-secret defaults only)

Required settings:

| Environment variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__SecretKey` | JWT signing key (long random string) |
| `EmailHost` | SMTP host (e.g. `smtp.gmail.com`) |
| `EmailUsername` | SMTP username / from-address |
| `EmailPassword` | SMTP password / app password |
| `AppSettings__PasswordResetUrl` | Frontend reset-password page URL used in reset emails |

Optional settings (defaults in `appsettings.json`):

| Setting | Default | Purpose |
|---|---|---|
| `Jwt__AccessTokenMinutes` | 60 | Access-token lifetime |
| `Jwt__RefreshTokenDays` | 7 | Refresh-token lifetime |
| `Attendance__LateThreshold` | `11:00` | Check-ins after this time are LATE |
| `CorsSettings__AllowOrigins__0` | `*` | Pin to the real frontend origin(s) in production |
| `LocationRange__*` | (Lagos office) | Geofence bounding box for mobile check-in |

For local development, copy real values into `appsettings.Local.json` (git-ignored).

> ⚠️ The credentials that were previously committed to this repository are in git
> history and **must be rotated**: the Gmail app password, the SQL Server login,
> and the JWT secret key.

### 2. Database migration

Run [Database/migrations/001_v2_upgrade.sql](Database/migrations/001_v2_upgrade.sql)
against the database **before deploying v2**. It is idempotent and additive
(adds `Notification.Id`, `Attendance_History.Id`/`Location`, the `RefreshTokens`
table, widens `PasswordResetTokens.Token`, adds indexes).

Also see [Database/002_recommended_security_audit.sql](Database/002_recommended_security_audit.sql):
pre-v2 initial admin passwords were derived from the Staff_ID and should be reset.

### 3. Run

```bash
dotnet run
```

Swagger UI is available at `/swagger` in Development. Health check: `GET /api/health` (anonymous).

## Auth model

- JWT bearer tokens; claims: `nameid` (staff id), `unique_name`, `LabRole` (`A1` super admin / `B2` sub admin / `C3` staff), `exp`.
- **Every endpoint requires a bearer token** except: `POST /api/User/loginAdmin`, `POST /api/User/loginuser`, `POST /api/PasswordReset/*`, `POST /api/Auth/refresh`, `GET /api/health`.
- Role policies: `Admin` = A1 or B2; `SuperAdmin` = A1 (approve/deny/remove users, send email).
- Sessions: access token (60 min) + rotating refresh token (`POST /api/Auth/refresh`), revoked by `POST /api/Auth/logout`, password change, or user removal.
- Login and password-reset endpoints are rate-limited (10/min per IP).

## Endpoint summary

| Area | Endpoints |
|---|---|
| Session | `POST /api/User/loginAdmin`, `POST /api/User/loginuser`, `POST /api/Auth/refresh`, `POST /api/Auth/logout`, `GET /api/User/me`, `POST /api/User/ChangePassword`, `POST /api/User/ConfirmPassword` |
| Password reset | `POST /api/PasswordReset/request-reset`, `POST /api/PasswordReset/verify-token`, `POST /api/PasswordReset/reset` |
| Users (Admin) | `GET /api/User/AddedUsers[?query=&page=&pageSize=]`, `POST /api/User/AddUser`, `GET /api/User/nonapproved`, `POST /api/User/approve`*, `POST /api/User/DenyUser`*, `POST /api/User/RemoveUser`*, `GET /api/User/employeesByRole`, `POST /api/User/ResetDevice` (* = SuperAdmin) |
| History (Admin) | `GET /api/User/ApprovalHistory`, `GET /api/User/DeletionHistory`, `DELETE /api/User/DeletionHistory/{staffId}` |
| Notifications (Admin) | `GET /api/User/GetNotification`, `GET /api/User/Notification/count`, `PUT /api/User/Notification/{id}/read` |
| Attendance capture | `POST /api/User/checkin` (mobile self, device+geofence), `POST /api/Attendance/CheckIn` (admin/kiosk), `PUT /api/Attendance/Checkout` / `POST /api/Attendance/CheckOut` |
| Attendance reads | `GET /api/Attendance/AttendanceHistory[?from=&to=&staffId=]`, `POST /api/Attendance/AttendanceByDate`, `GET /api/Attendance/Summary?date=`, `POST /api/Attendance/AttendanceByID`, `GET|POST /api/Attendance/Latecheckin`, between-dates variants |
| Leave | `POST /api/Leave/request`, `GET /api/Leave/Getrequests` (Admin), `POST /api/Leave/approve` (Admin) |
| Misc | `GET /api/health`, `GET /api/LocationRange`, `GET /api/Image/{staffId}`, `POST /api/Image/UploadImage` (multipart), `POST /api/Email` (SuperAdmin) |

Error responses are JSON: `{ "success": false, "message": "..." }` with appropriate
status codes (400/401/403/404/409/429/500).

## Breaking changes in v2 (for client apps)

1. **Bearer token now required** on all non-auth endpoints (the admin portal already sends it; the mobile app must attach the token from `loginuser` to `checkin`/`Checkout`).
2. `loginAdmin`/`loginuser` responses now include `refreshToken` alongside `token`.
3. Wrong credentials return **401** with a JSON `{ message }` (previously 400 plain text).
4. `AddUser` returns JSON `{ success, message, staff_ID }` (previously plain text); duplicates return **409**.
5. Double check-in returns **409** instead of silently overwriting the record; checkout without a check-in returns **409**.
6. `GetNotification` returns `{ id, staff_ID, roleID, isRead, message, name }` filtered to the caller's role.
7. `AttendanceByDate`/history rows now include `id`, `name`, and `location`.
8. Device binding: first `loginuser` binds the device; a different device is rejected until an admin calls `ResetDevice`.

## Notes

- Attendance times are serialized as `"HH:mm:ss"`; dates as ISO `"yyyy-MM-ddTHH:mm:ss"`.
- The Leave and Image features still call stored procedures (`InsertLeaveRequest`, `GetLeaveRequests`, `ApproveLeaveRequest`, `InsertImage`, `GetImageById`) that live only in the database — export them to source control (see `Database/002_recommended_security_audit.sql`).
- Remaining known gaps are tracked in [GAPS_AND_IMPROVEMENTS.md](GAPS_AND_IMPROVEMENTS.md).
