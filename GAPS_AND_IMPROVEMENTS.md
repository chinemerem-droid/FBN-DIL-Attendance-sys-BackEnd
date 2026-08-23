# FBN-DIL Attendance System - Gaps, Issues & Proposed Features

**Document Version:** 2.2
**Date:** August 23, 2026
**Project:** Employee Attendance Management System Backend
**Scope:** v2.0 re-verified every v1.0 issue against the code, corrected one (#3), added 20 new findings (N1–N20), and reconciled against the frontend's `docs/API-ENDPOINTS.md` (Part C). v2.1 implemented the P0/P1 roadmap. v2.2 restructured the codebase feature-based.

---

## ✅ v2.1 — Implementation Status (backend rewrite, August 23, 2026)

The P0 and P1 roadmap items in Part D have been **implemented and the build verified** (0 errors, 0 warnings; smoke-tested end-to-end against a LocalDB test database — see `README.md` and `Database/setup-localdb.ps1`).

**Fixed in this build:** N1 (device rebind on `loginuser`), N2 (deterministic initial passwords → CSPRNG), N3 (email relay → SuperAdmin only), N4/#4 (auth required everywhere by default + `Admin`/`SuperAdmin` role policies), N5 (role check on `loginAdmin`), N6 (`approve` looks up email server-side, mails after commit, non-fatal), N7/#31 (DTOs — no more password hashes in responses), N8 (MessageRead → `PUT Notification/{id}/read`), N9/#20 (double check-in → 409, never overwrites), N10 (AddUser real status codes, 409 on duplicate), N11 (RemoveUser sets `RemovalDate`), N12 (UploadImage multipart binding + size/type limits), N13/N14 (notification `id` + `name`, role-filtered), N15 (Checkout authenticated + requires check-in), N16 (60-min tokens + rotating refresh tokens, logout revocation), N17/N18 (JSON `{message}` envelopes, 401s), N19/N20 (single route, `ControllerBase`, template files removed), #1 (secrets → env vars / git-ignored `appsettings.Local.json`), #2 (32-byte single-use reset tokens with enforced expiry), #6 (CORS handled correctly, pinnable via env), #7 (rate limiting on auth endpoints), #24 (middleware order fixed), #25 (scoped `SqlConnection` disposed by DI), #26 (global exception middleware), #29-partial (test packages removed from web project), #32/#33-partial (optional pagination + search on users, from/to/staffId filters on attendance history), #35 (`GET /api/health`), #43-partial (Dockerfile added), and new endpoints: `me`, `ChangePassword`, `verify-token`, `Auth/refresh`, `Auth/logout`, `Attendance/Summary`, `Notification/count`, `DELETE DeletionHistory/{staffId}`, `ResetDevice`.

**v2.2 restructure (same day):** codebase reorganized into a feature-based layout (`Common/` + `Features/{Auth, Users, Notifications, Attendance, Leave, PasswordReset, Images, Email, Location}`), the `DapperUser` god-repository split into per-feature repositories, all request bodies replaced with slim validated DTOs (resolves #18 basic validation and #23 naming), and every controller/action documented with XML summaries surfaced in Swagger. All legacy routes preserved; the project now targets **.NET 10 LTS**.

**Required before deploying:**
1. Run `Database/migrations/001_v2_upgrade.sql` (additive, idempotent — already validated against a legacy-shaped LocalDB).
2. Set environment variables (see README) and **rotate** the previously committed credentials (Gmail app password, SQL login, JWT secret — all in git history).
3. Mobile app must send its bearer token on `checkin`/`Checkout` (breaking changes listed in README).

**Still open:** #9 audit trail, #12 full reports (only Summary exists), #13 leave balances, #14 shifts/holidays, #15 notification delivery (SignalR/email), #16 radius geofencing, #17 overtime, #21 timezone strategy, #22 FK audit, #27 structured logging, #28 role codes hard-coded, #29 tests, #30 versioning, #34 full-text search, #36–42 (exports, import, caching, indexing beyond migration 001, background jobs, CI/CD), #44–46 (procs for Leave/Image still only in DB — export via `Database/002_recommended_security_audit.sql`). Enhancements 48–55 unchanged.

---

## Part A — v1.0 Issues: Verified Status (as of v2.0 audit)

Legend: ✅ was confirmed open · ✏️ corrected (v1.0 was wrong/partial) · 🔧 fixed in v2.1/v2.2

### Critical Security Issues

| # | Issue | Status | Notes |
|---|---|---|---|
| 1 | Secrets in `appsettings.json` | 🔧 Fixed (rotate still pending) | Moved to env vars / git-ignored `appsettings.Local.json`. Old credentials remain in git history — **rotate them**. |
| 2 | Weak password reset token | 🔧 Fixed | Was 5 chars with no expiry check; now 32-byte single-use tokens, 1-hour expiry enforced, rate-limited. |
| 3 | "Broken" password hashing | ✏️ Corrected | v1.0 claimed the salt was never stored — wrong; it was embedded in the stored string and verification worked. v2.1 bumped PBKDF2 to 100k iterations (legacy hashes still verify). |
| 4 | Missing `[Authorize]` | 🔧 Fixed | Fallback policy requires auth everywhere; `Admin` (A1/B2) and `SuperAdmin` (A1) policies applied per endpoint. |
| 5 | Input validation gaps | 🔧 Mostly fixed | Slim request DTOs with validation attributes (v2.2); validation errors return the standard `{ success, message }` envelope. |
| 6 | CORS misconfiguration | 🔧 Fixed | `"*"` handled correctly (AllowAnyOrigin, no credentials); pin production origins via `CorsSettings__AllowOrigins__0`. |
| 7 | No rate limiting | 🔧 Fixed | 10 req/min per IP on login, refresh, confirm-password, and password-reset endpoints (429). |
| 8 | Device binding spoofable | 🔧 Fixed | Was worse than documented (N1). First login binds; mismatched devices are rejected; admin `ResetDevice` re-binds. |

### Missing Core Functionalities

| # | Issue | Status |
|---|---|---|
| 9 | No audit trail | ✅ Open |
| 10 | No attendance correction workflow | ✅ Open (destructive re-check-in fixed — N9) |
| 11 | No bulk operations | ✅ Open |
| 12 | No reports/analytics | Partial — `GET /api/Attendance/Summary` added; full reports open |
| 13 | No leave balance management | ✅ Open |
| 14 | No shift/schedule management | ✅ Open (late threshold now configurable: `Attendance:LateThreshold`) |
| 15 | No notification delivery | Partial — read-state + count + role filtering fixed; no push/email delivery |
| 16 | Rectangular geofence | ✅ Open (single bounding box; no radius/multi-site) |
| 17 | No overtime tracking | ✅ Open |

### Data Integrity & Validation

| # | Issue | Status |
|---|---|---|
| 18 | Missing input validation | 🔧 Fixed (v2.2 DTOs with attributes) |
| 19 | No transaction management | 🔧 Improved — approve/deny/add wrapped; emails sent after commit, non-fatal |
| 20 | Duplicate check-in | 🔧 Fixed — second check-in returns 409, never overwrites |
| 21 | Date/time inconsistency | ✅ Open (local time for attendance, UTC for auth/tokens; no timezone strategy) |
| 22 | No FK/reference validation | ✅ Open |

### Architecture & Code Quality

| # | Issue | Status |
|---|---|---|
| 23 | Inconsistent naming (`DappaRepo` etc.) | 🔧 Fixed (v2.2 feature-based layout) |
| 24 | Duplicate/wrong middleware order | 🔧 Fixed |
| 25 | `SqlConnection` lifecycle | 🔧 Fixed — scoped, DI-disposed |
| 26 | No error-handling middleware | 🔧 Fixed — global handler, JSON envelope, no stack traces |
| 27 | No request/response logging | ✅ Open (console logging only) |
| 28 | Hard-coded business logic | Partial — late threshold configurable; role codes still literals |
| 29 | No tests | ✅ Open (test packages removed from web project; no test project yet) |
| 30 | No API versioning | ✅ Open |
| 31 | No response DTOs | 🔧 Fixed — password hashes/device ids no longer serialized |

### Missing API Features

| # | Issue | Status |
|---|---|---|
| 32 | No pagination | 🔧 Added (opt-in on `AddedUsers`; plain array preserved for current frontend) |
| 33 | No filtering | 🔧 Added (from/to/staffId on `AttendanceHistory`) |
| 34 | No search | Partial — `?query=` on users; no full-text |
| 35 | No health check | 🔧 Fixed — `GET /api/health` (DB connectivity) |
| 36 | No file export | ✅ Open |
| 37 | No batch import | ✅ Open |

### Performance / DevOps (38–47)

38–41 (caching, N+1 review, further indexing, background jobs) ✅ open. 42 CI/CD ✅ open. 43 Dockerfile 🔧 added (.NET 10 images; no compose). 44 env-specific config 🔧 fixed. 45 monitoring ✅ open. 46 DB migrations 🔧 started (`Database/migrations/`, idempotent; Leave/Image stored procs still only in the DB — export them). 47 README 🔧 added.

### Proposed enhancements (48–55)
Unchanged: mobile app support, manager dashboard, self-service portal, integrations, advanced analytics, biometrics, flexible work, compliance.

---

## Part B — New Findings from the v2.0 audit (all fixed in v2.1)

- **N1** Passwordless `loginuser` allowed device-binding takeover → first-login binding + mismatch rejection + admin `ResetDevice`.
- **N2** Initial admin passwords derived from `SHA256(Staff_ID)` → CSPRNG passwords. **Audit pre-v2 admin accounts** (`Database/002_recommended_security_audit.sql`).
- **N3** `POST /api/Email` was an unauthenticated open relay → SuperAdmin only.
- **N4** Every state-changing endpoint was anonymous → fallback auth policy.
- **N5** `loginAdmin` had no role check → A1/B2 enforced.
- **N6** `approve` crashed (500) when called with only `{Staff_ID}` (null email parsed before DB update) → server-side email lookup, mail after commit, non-fatal.
- **N7** Password hashes + device IDs serialized in user endpoints → DTOs.
- **N8** `MessageRead` had a SQL syntax error (always 500) → `PUT /api/User/Notification/{id}/read`.
- **N9** Re-check-in overwrote `EntryTime`/status → 409 conflict.
- **N10** `AddUser` returned 200 on failure → real status codes, 409 on duplicate.
- **N11** `RemoveUser` never set `RemovalDate` (DeletionHistory permanently empty) → fixed; also revokes sessions.
- **N12** `UploadImage` could never bind (`IFormFile` + `[FromBody]`) → multipart form + size/type limits.
- **N13/N14** Notifications had no `id`/`name` and no stable record ids anywhere → migration 001 adds identity columns; DTOs expose them.
- **N15** Checkout anonymous/unvalidated → authenticated, own-record, requires a check-in.
- **N16** 20-minute JWTs with no refresh → 60-minute tokens + rotating refresh tokens + logout revocation.
- **N17/N18** Plain-text errors, 400 for wrong password → JSON `{success, message}` envelopes, 401s.
- **N19/N20** Duplicate routes, `Controller` base class, template leftovers → cleaned up.

---

## Part C — Frontend Contract Reconciliation (v2.0 audit → now resolved)

All 13 endpoints the AMS Admin Portal calls exist and match their expected contracts, including the two that were broken (`approve`, `DELETE DeletionHistory/{id}`) and the two with mismatched shapes (`GetNotification`, `ApprovalHistory`). Answers to the frontend's contract questions:

1. **Formats:** `entryTime`/`exitTime` serialize as `"HH:mm:ss"`; `date` as ISO `"yyyy-MM-ddTHH:mm:ss"`; request `{ "date": "YYYY-MM-DD" }` binds fine.
2. **`GetNotification`:** `{ id, staff_ID, roleID, isRead, message, name }`, filtered to the caller's role. The old `sen1..sen4` fields never existed.
3. **History ids:** `id` = staff id (stable) on approval/removal records; `DELETE /api/User/DeletionHistory/{staffId}` clears both dates.
4. **Error envelope:** always JSON `{ success, message }`; wrong credentials → **401** (was 400 text); `ConfirmPassword` wrong password → **401**.
5. **Auth:** everything requires `Authorization: Bearer` except `loginAdmin`, `loginuser`, `PasswordReset/*`, `Auth/refresh`, `/api/health`.
6. **`AddUser`:** returns `{ success, message, staff_ID }`; duplicate → 409.
7. **Requested endpoints delivered:** `me`, `logout`, `refresh`, `ChangePassword`, `verify-token`, notification read/count, pagination/search, `Summary`, `health`. Attendance capture existed all along (`/api/User/checkin`, `/api/Attendance/Checkout`) — now with stored `location` and proper auth. Frontend follow-up: send the bearer token from `loginuser` on mobile capture calls, and store/use `refreshToken`.

---

## Part D — Remaining Roadmap

### P2 — Hardening & completeness
1. User lifecycle: `GET/PUT/DELETE /api/User/{staffId}` incl. role changes.
2. Full reports (late arrivals over time, absences, per-staff summaries), exports (CSV/Excel).
3. Structured logging (Serilog) + request logging; monitoring/APM.
4. Export the Leave/Image stored procedures into `Database/` and adopt a migration tool (DbUp/FluentMigrator).
5. Unit + integration tests; CI/CD pipeline.

### P3 — Product depth
6. Audit trail (#9), leave balances (#13), shifts/holiday calendar (#14), notification delivery (SignalR/email) (#15), radius geofencing + multi-site (#16), overtime (#17), timezone strategy (#21), API versioning (#30), caching/background jobs (#38–41).

---

**Document maintained by:** Development Team
**Last updated:** August 23, 2026 (v2.2 — feature-based restructure)
**Next review:** After P2 planning
