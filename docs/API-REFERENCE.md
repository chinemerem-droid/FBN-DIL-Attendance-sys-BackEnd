# AMS Backend — API Reference (for the Frontend)

**Backend version:** v2.2 (feature-based, .NET 10) · **Date:** August 23, 2026

## Base URLs

| Environment | URL |
|---|---|
| **Local** | `http://localhost:5202` (default `dotnet run` port) |
| Local Swagger UI | `http://localhost:5202/swagger` (interactive docs — the root `/` redirects here in dev) |
| Production | `https://attsystem-latest.onrender.com` (`REACT_APP_API_URL`) — redeploy required for v2 |

**Local test login:** `ADMIN001` / `Admin@123!` (super admin, LocalDB test database only).

## Auth model

- JWT bearer token from `loginAdmin` / `loginuser`, sent as `Authorization: Bearer <token>` .
- JWT claims: `nameid` (staff id), `unique_name` (name), `LabRole` (`A1` super admin / `B2` sub admin / `C3` staff), `exp`.
- Access tokens live **60 minutes**. Logins also return a **`refreshToken`** — exchange it at `POST /api/Auth/refresh` for a new pair (the old refresh token is revoked on use).
- **Every endpoint requires the bearer token** except: the two logins, `PasswordReset/*`, `Auth/refresh`, and `/api/health`.
- Roles: **Admin** = A1 or B2 · **SuperAdmin** = A1 only. Staff (C3) can only access their own data.

## Conventions

- **Errors** are always JSON: `{ "success": false, "message": "..." }` with proper status codes: `400` invalid input · `401` bad credentials/expired token · `403` wrong role or someone else's data · `404` not found · `409` conflict (duplicate user, double check-in) · `429` rate-limited · `500` server error.
- **Rate limiting:** login, refresh, confirm-password and password-reset endpoints allow **10 requests/minute per IP**, then `429`.
- **Formats:** times are `"HH:mm:ss"` strings; dates are ISO `"yyyy-MM-ddTHH:mm:ss"`; request dates accept `"YYYY-MM-DD"`. JSON fields are camelCase (`staff_ID`, `lab_role`, `entryTime`).

---

## 1. Auth & session

| Method | Path | Auth | Body | Returns |
|---|---|---|---|---|
| POST | `/api/User/loginAdmin` | none ⏱ | `{ staff_ID, password }` | `200 { message, token, refreshToken }` · `401` |
| POST | `/api/User/loginuser` | none ⏱ | `{ staff_ID, deviceID, deviceModel }` | `200 { message, token, refreshToken }` · `401` (unknown id / not approved / wrong device). First login binds the device. |
| POST | `/api/Auth/refresh` | none ⏱ | `{ refreshToken }` | `200 { message, token, refreshToken }` (rotated) · `401` |
| POST | `/api/Auth/logout` | bearer | optional `{ refreshToken }` | `200`. With a token: revokes that session; without: revokes all the caller's sessions. |
| POST | `/api/User/ConfirmPassword` | bearer ⏱ | `{ staff_ID, password }` | `200` ok · `401` wrong password |
| POST | `/api/User/ChangePassword` | bearer | `{ currentPassword, newPassword }` (min 8) | `200` (all sessions revoked) · `401` wrong current password |

⏱ = rate-limited (10/min/IP)

## 2. Current user

| Method | Path | Auth | Returns |
|---|---|---|---|
| GET | `/api/User/me` | bearer | `200 { staff_ID, name, email, phone_number, lab_role, approvalStatus, approvalDate }` |

## 3. User management (Admin)

| Method | Path | Auth | Body / Query | Returns |
|---|---|---|---|---|
| POST | `/api/User/AddUser` | Admin | `{ staff_ID, name, email, phone_number, lab_role }` | `200 { success, message, staff_ID }` · `400` invalid · `409` duplicate staff id |
| GET | `/api/User/AddedUsers` | Admin | optional `?query=&page=&pageSize=` | `200` UserDto[] (no params, current frontend shape) or `{ data, totalCount, page, pageSize }` |
| GET | `/api/User/nonapproved` | Admin | — | `200` UserDto[] (pending approval) |
| GET | `/api/User/employeesByRole` | Admin | `?Lab_role=A1\|B2\|C3` | `200` UserDto[] |
| POST | `/api/User/approve` | **SuperAdmin** | `{ staff_ID }` | `200` · `404` unknown user. Sends approval email (non-fatal). |
| POST | `/api/User/DenyUser` | **SuperAdmin** | `{ staff_ID }` | `200` (pending registration rejected + deleted) |
| POST | `/api/User/RemoveUser` | **SuperAdmin** | `{ staff_ID }` | `200` (offboarded: un-approved, RemovalDate set, sessions revoked) · `404` |
| POST | `/api/User/ResetDevice` | Admin | `{ staff_ID }` | `200` (user can bind a new device on next mobile login) · `404` |
| GET | `/api/User/ApprovalHistory` | Admin | — | `200` `[{ id, staff_ID, name, approvalStatus, date }]` (`id` = staff id) |
| GET | `/api/User/DeletionHistory` | Admin | — | `200` `[{ id, staff_ID, name, email, date }]` |
| DELETE | `/api/User/DeletionHistory/{staffId}` | Admin | — | `200` (clears that user's history dates) · `404` |

`UserDto` = `{ staff_ID, name, email, phone_number, lab_role, approvalStatus, approvalDate }` — never contains password hashes or device ids.

## 4. Notifications (Admin)

Notifications are addressed to a role; each caller sees their own role's list.

| Method | Path | Auth | Returns |
|---|---|---|---|
| GET | `/api/User/GetNotification` | Admin | `200` `[{ id, staff_ID, roleID, isRead, message, name }]` newest first |
| GET | `/api/User/Notification/count` | Admin | `200 { count }` (unread, for the badge) |
| PUT | `/api/User/Notification/{id}/read` | Admin | `200` · `404` unknown id |

## 5. Attendance

`AttendanceRecord` = `{ id, staff_ID, name, entryTime "HH:mm:ss", exitTime, date, location "lat,long", checkinStatus "ON TIME"|"LATE" }` (`exitTime`/`location` may be null).

### Capture

| Method | Path | Auth | Body | Returns |
|---|---|---|---|---|
| POST | `/api/User/checkin` | bearer (mobile) | `{ deviceID, deviceModel, latitude, longitude }` (staff_ID optional, admins only) | `200` record · `401` device mismatch · `400` outside geofence · `409` already checked in today |
| POST | `/api/Attendance/CheckIn` | Admin | `{ staff_ID, location? }` | `200` record · `409` already checked in (kiosk/manual override — no device/geofence check) |
| PUT | `/api/Attendance/Checkout` (alias: POST `/api/Attendance/CheckOut`) | bearer | `{ staff_ID? }` (own if omitted; admins may target anyone) | `200` record · `409` no check-in today |

### Reads

| Method | Path | Auth | Body / Query | Returns |
|---|---|---|---|---|
| GET | `/api/Attendance/AttendanceHistory` | bearer | optional `?from=&to=&staffId=` | `200` AttendanceRecord[] newest first. Non-admins always get only their own. |
| POST | `/api/Attendance/AttendanceByDate` | Admin | `{ date: "YYYY-MM-DD" }` | `200` AttendanceRecord[] for that day (Home page poll) |
| GET | `/api/Attendance/Summary` | Admin | optional `?date=` (default today) | `200 { date, totalEmployees, present, checkedOut, late }` |
| POST | `/api/Attendance/AttendanceByID` | bearer* | `{ staff_ID }` | `200` latest record · `404` |
| POST | `/api/Attendance/GetAttendanceByIDandDate` | bearer* | `{ staff_ID, date }` | `200` record · `404` |
| POST | `/api/Attendance/GetAttendanceByIDbtwDates` | bearer* | `{ staff_ID, startDate, endDate }` | `200` AttendanceRecord[] |
| POST | `/api/Attendance/GetAttendancebtwDates` | Admin | `{ startDate, endDate }` | `200` AttendanceRecord[] |
| GET/POST | `/api/Attendance/Latecheckin` | Admin | — | `200` AttendanceRecord[] with checkinStatus "LATE" |

\* staff may only query their own staff_ID; admins may query anyone (`403` otherwise).

## 6. Leave

| Method | Path | Auth | Body | Returns |
|---|---|---|---|---|
| POST | `/api/Leave/request` | bearer | `{ startDate, endDate }` (staff_ID optional, admins only) | `200` · `400` endDate before startDate |
| GET | `/api/Leave/Getrequests` | Admin | — | `200` `[{ id, staff_ID, startDate, endDate, status }]` |
| POST | `/api/Leave/approve` | Admin | `{ staff_ID }` | `200` |

## 7. Password reset (anonymous, rate-limited ⏱)

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/PasswordReset/request-reset` | `{ email }` | `200` always (never reveals whether the email exists). Token emailed, single-use, expires in 1 hour. |
| POST | `/api/PasswordReset/verify-token` | `{ email, token }` | `200` valid · `400` invalid/expired — call before showing the new-password screen |
| POST | `/api/PasswordReset/reset` | `{ email, token, newPassword }` (min 8) | `200` (sessions revoked) · `400` invalid/expired token |

## 8. Images

| Method | Path | Auth | Body | Returns |
|---|---|---|---|---|
| POST | `/api/Image/UploadImage` | bearer* | multipart/form-data: `file` (JPEG/PNG ≤ 5 MB) + `staff_ID` | `200` image bytes · `400` invalid file |
| GET | `/api/Image/{staffId}` | bearer* | — | `200` image bytes · `404` |
| POST | `/api/Image` | bearer* | `{ staff_ID }` (legacy) | `200` image bytes · `404` |

\* own image only, unless admin.

## 9. Misc

| Method | Path | Auth | Returns |
|---|---|---|---|
| GET | `/api/health` | none | `200 Healthy` / `503 Unhealthy` (DB connectivity) — use this for availability checks instead of inferring from 503s |
| GET | `/api/LocationRange` | bearer | `200 { minLongitude, maxLongitude, minLatitude, maxLatitude }` (office geofence for the mobile app) |
| POST | `/api/Email` | **SuperAdmin** | `{ to, subject, body }` → `200` |
| GET | `/` | none | dev: redirects to `/swagger` · prod: `{ name, status, health }` |

---

## Migration notes for the current frontend code

1. **Store `refreshToken`** from login and call `/api/Auth/refresh` before `exp` — sessions no longer hard-die.
2. Login/credential failures now return **401 with JSON `{ message }`** (previously 400 plain text) — `error.response.data.message` works everywhere now.
3. `AddUser` returns JSON `{ success, message, staff_ID }` (no longer text) and **409** on duplicates.
4. `GetNotification` items now have real `id` and `name`; use `PUT Notification/{id}/read` and the `count` endpoint for the badge.
5. `DeletionHistory` delete uses the **staff id** in the URL: `DELETE /api/User/DeletionHistory/{staffId}`.
6. Attendance rows include `id`, `name`, and `location` — the Home Location column can populate.
7. Mobile app: attach the bearer token on `checkin`/`Checkout`; handle `409` ("already checked in" / "no check-in today").
