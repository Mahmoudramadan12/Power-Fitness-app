# What changed and why

## Bugs fixed (present in the original)

| # | Problem | Fix |
|---|---|---|
| 1 | **Editing a member never worked** – the form posted to a non-existent `Members` controller | Rewritten; verified by an end-to-end test that edits a member |
| 2 | **Deleting a trainer silently deleted all their past sessions, bookings and attendance** (FK was `CASCADE`) | FKs are `RESTRICT`; a trainer with any session on record can't be deleted |
| 3 | **Production DB password committed** in `appsettings.Production.json` | Removed; configuration comes from env vars/user-secrets. **Rotate that password.** |
| 4 | Seeded admin accounts shared a hardcoded password and **never received a role** | Accounts/passwords come from configuration; roles assigned; nothing seeded in production without a password |
| 5 | No antiforgery validation on any POST (CSRF) | Global `AutoValidateAntiforgeryToken` |
| 6 | `Cardio` and `Strength` sessions could never be scheduled; General-Fitness trainers couldn't lead anything (`specialty == category name`) | `TrainerCategoryRules`: General Fitness → Cardio + Strength |
| 7 | Photo delete used a different folder than upload – photos were never removed | One resolver for upload/read/delete; path-traversal safe |
| 8 | Upload validation was extension-only and case-sensitive | Extension **and** file-signature check, 5 MB cap, generated names |
| 9 | Capacity validation allowed 0 but the DB check rejects it → 500 error | Range 1–25 on the view model |
| 10 | Address length limits in the UI (100/150) exceeded the DB columns (30) → truncation/500 | Columns widened and aligned |
| 11 | Names/streets restricted to ASCII letters; DB columns were `varchar` (Arabic would become `?`) | Unicode validation, `nvarchar` columns |
| 12 | Membership start date was stored as "now" even when a future date was chosen | Start date stored as the real start; status is Upcoming/Active/Expired |
| 13 | Attendance could be marked on any session at any time; members without a valid membership could be booked | Attendance only while running; booking requires membership covering the session date |
| 14 | Booking capacity check had a race condition | Serializable transaction + retry |
| 15 | Trainers could be double-booked | Overlap check on create/update |
| 16 | Upcoming sessions with no bookings could not be deleted | Allowed (running sessions and sessions with bookings still protected) |
| 17 | `/Home/Error` referenced but missing; `/Privacy` view without action; stray Razor `PageModel` | Real error + status-code pages |
| 18 | `MapStaticAssets()` called twice; broken refs to Font Awesome, Select2, missing `default-avatar.png` | Clean pipeline; all assets self-hosted; generated initials avatars |
| 19 | N+1 queries (booked count per session, active plan per member) | One grouped query per page |
| 20 | `GetAllSessions` returned `null` when empty | Always returns a list |
| 21 | Active-member KPI counted memberships, not members | Distinct members |
| 22 | Heights/weights stored as `decimal(18,2)`; no unique plan names | `decimal(6,2)`, unique index |
| 23 | Landing page was a copy-pasted template ("+91" phone number, three identical trainers, dead buttons); brand was "Power fitness" in one place and "Route Fitness" in another | Real dashboard; one brand (`Gym:Name`) |

## Security hardening
Antiforgery everywhere · login rate limiting + lockout + uniform error message · open-redirect-safe `returnUrl` ·
secure/HttpOnly cookies, 8 h sliding expiry · security headers + strict CSP (no inline scripts, no CDNs) ·
uploads outside `wwwroot` and served only to signed-in users · password policy (8+ chars) and a change-password page ·
`Trainer` pages were hidden in the menu but reachable by URL – now enforced server-side.

## Behaviour changes you might notice
- **Permissions:** Trainers pages are Super-admin only (matching the menu); creating/editing/activating **plans** is Super-admin only.
- **Photo is optional** (initials avatar is shown otherwise); a photo can now be replaced on edit.
- **Name is editable** on members/trainers (typo fixes). Date of birth and gender remain fixed.
- **"Cancel membership"** on an *active* membership now ends it today and keeps the record (and its price) for history; a membership that hasn't started is removed.
- **A member can hold one membership at a time** (a future one can be queued after the current one ends).
- Session **category can't change** after creation; a session with bookings can't be edited.
- Dates use the **gym's time zone** (`Gym:TimeZone`), not the server's.
- AutoMapper was replaced with explicit, compile-checked mapping (fewer moving parts; AutoMapper 15+ also needs a licence key).

## New
Dashboard with charts · search + pagination everywhere · session status tabs · member profile with history, health record and
bookings · health-record editing · plan creation · membership price snapshot · demo data · `/health` endpoint ·
Docker + compose · CI workflow · 76 automated tests.

## Known limitations / good next steps
- Member/trainer deletion is a hard delete (members' history goes with them). Soft-delete would be the next step if you need audit trails.
- No payment capture or refunds – revenue is "price paid at membership start".
- No user-management screen for admins (accounts are created through `Seed:*`).
- Arabic text is stored and validated correctly, but the UI is English/LTR only.
