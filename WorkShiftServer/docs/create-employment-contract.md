# Create employment contract (Branch Manager)

POST `/api/manager/contracts`, bearer JWT belonging to an ACTIVE, non-deleted MANAGER with an assigned branch. ADMIN alone is not authorized. Swagger uses the existing Bearer security definition.

Request (replace the UUID with a STAFF in the manager's branch):
```json
{
  "staffUserId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "startDate": "2026-10-01",
  "endDate": null,
  "employeePosition": "Thu ngân",
  "contractTerms": "Điều khoản hợp đồng"
}
```

Illustrative HTTP 201 response (IDs, names and rate depend on database configuration):
```json
{
  "contractId": 123,
  "contractNumber": "EC-2c7228db01154bff871d5bec8801c34e",
  "staffUserId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "employeeFullName": "Nguyễn Văn A",
  "branchId": 3,
  "branchName": "Chi nhánh A",
  "regionId": 5,
  "regionName": "Vùng A",
  "startDate": "2026-10-01",
  "endDate": null,
  "hourlyRate": 25000,
  "employeePosition": "Thu ngân",
  "contractTerms": "Điều khoản hợp đồng",
  "status": "DRAFT",
  "createdBy": "11111111-2222-3333-4444-555555555555",
  "createdAt": "2026-10-01T12:00:00Z"
}
```

## Validation and persistence

- BLL rechecks the actor's MANAGER role, ACTIVE account/role, deletion state and branch assignment.
- Staff must exist, not be deleted, have STAFF role, be PENDING or ACTIVE, and belong to the manager's branch. Branch must be ACTIVE and region active.
- StartDate is required; optional EndDate must be strictly later. Past dates are allowed. Position must contain non-whitespace characters and be at most 100 characters; surrounding whitespace is trimmed on storage.
- Uses only Region.RegionalMinimumWageId and its referenced configuration. Wage must be ACTIVE, positive, have EffectiveFrom, and cover StartDate. Both effective bounds are inclusive; null EffectiveTo means no end. No fallback wage or region update.
- Copies employee/branch/region snapshots and the hourly rate. Creates DRAFT with UTC timestamps, generated UUID-based contract number (35 characters), database-generated identity, null replacement/lifecycle/PDF fields, and no salary history.
- Existing schema already has `uq_one_pending_contract_per_staff` covering DRAFT, PENDING_STAFF, PENDING_MANAGER_APPROVAL. BLL checks it and DAL maps concurrent violations to HTTP 409. No additional date-overlap rule was found in the checked-in schema or existing contract code.
- Saves the contract and a CREATE audit_logs entry in one explicit EF transaction. Audit failure rolls back the contract. Existing Admin list/detail endpoints are unchanged. No schema change or migration.

Business errors use `{ "message": "..." }`: 400 invalid data/location/wage, 401 missing or invalid identity, 403 missing MANAGER authorization or wrong branch, 404 missing/deleted Staff, 409 existing pending contract. Framework authentication/authorization and automatic malformed-body validation retain their normal ASP.NET responses.

## Verification

Run `dotnet test WorkShiftManagement.Tests/WorkShiftManagement.Tests.csproj` from WorkShiftServer. The new xUnit/Moq project uses isolated repository mocks; it does not connect to Supabase, send mail or start API background workers. Tests cover BLL validation, snapshots, server-owned fields, distinct numbers, controller error mapping/201/role metadata, and existing Admin detail behavior.

Live JWT middleware, PostgreSQL identity/unique-index enforcement, concurrent inserts and transaction rollback require an isolated PostgreSQL integration environment and have not been exercised by these unit tests. Do not run mutation checks against production.
