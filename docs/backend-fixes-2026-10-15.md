# Scheduling and account management fixes

- Scheduling uses Asia/Ho_Chi_Minh. Creation rejects an elapsed slot, including today; mixed submissions containing an elapsed slot fail entirely. Past dates no longer appear in the doctor's working calendar. Appointment/history records remain available.
- Five NO_SHOW appointments in the thirty calendar days including today notify active admins, once when the threshold is crossed. Counts belong to the booking account (including dependent profiles), not the profile ID. A doctor must own the appointment and its start time must have passed. No automatic account blocking.
- Blocked/revoked cookies are cleared during navigation. Public pages remain accessible as a guest; protected pages redirect to login. Protected APIs still reject blocked accounts.
- `/Auth/Appeal` verifies the existing password without granting an authenticated session. A blocked patient/doctor can submit a reason and check status/ban reason. One pending appeal per account, at most one submission per 24 hours; credential endpoints have an IP rate limit. `/Admin/Appeals` requires an administrator; approval/rejection requires a reason and stores time/admin ID. Rejected users read the outcome on the public status form. Approval and direct unlock also create inbox notifications. Direct unlock now requires a `reason` query parameter.
- Transfer requests snapshot the old facility's name. The migration backfills only still-pending requests; older completed requests without a snapshot show “Chưa ghi nhận”, because their original facility cannot be reliably inferred. The API exposes processing time for approved/rejected tabs.
- Notification DELETE endpoints only remove the current user's already-read records. The inbox supports bulk deletion and scrolling.
- Admin doctor, specialty and facility search is performed in PostgreSQL and supports Vietnamese without accents. Approved inventory count and list use the same predicate, including blocked accounts. Account role and professional-profile approval remain distinct.
- Zero reviews displays “Chưa có đánh giá” across admin, home and doctor pages; it does not default to five stars. Hidden review content continues to contribute to rating.

## Deployment

The `AddAccountAppealsAndTransferOrigin` migration is required. The application applies migrations at startup. Rebuild/redeploy the application after pulling code; changes in this workspace do not update the VPS automatically.

## Verification

`tests/BackendRegression` passes on an isolated PostgreSQL database. Browser checks also exercised stale blocked cookies, public appeal submission, administrator approval with reason, status lookup and login after unlock, admin search pages, scrolling and deleting read notifications.
