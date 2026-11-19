# Phase 0 — Identity, Governance, and Data-Control Baseline

## Status and scope

**Status:** agreed implementation baseline; no database or application behaviour change is made by this phase.

This document defines the mandatory design decisions for the first security release of Summary Studio. It covers local identity, authorisation, ownership, audit, retention, and storage governance. It deliberately does not select an AI/OCR replacement; that decision is deferred until the secure access foundation exists.

## Decisions

### 1. Identity source

The initial release will use **local ASP.NET Core Identity backed by the existing SQL Server database**.

- The application is internally deployed and has no public registration endpoint.
- User accounts are created by an authorised application administrator only.
- The design must preserve a later migration path to the bank's SSO/Active Directory provider. Application-specific roles, client/case access, audit records, and ownership remain in this application even if authentication moves to SSO.
- Each person receives an individual account. Shared accounts are prohibited except for a separately controlled emergency access account, if bank policy requires one.

### 2. Authentication model

- One login endpoint and one authenticated web session; there is no separate admin login mechanism.
- Use ASP.NET Core Identity password hashing and security stamps. Do not implement custom password hashing or password encryption.
- Use server-issued, encrypted, `HttpOnly`, `Secure`, `SameSite` cookies. Do not store bearer tokens in browser local storage.
- Require HTTPS in production.
- Enforce rate limiting and a temporary lockout after repeated failed login attempts.
- Newly created accounts require a password change before normal access.
- Password-reset and MFA enrolment are Phase 1.3 work, after the base login and user-management controls are verified.
- The existing `demo-admin` shared secret is removed as an authorisation mechanism. The route it currently protects becomes a normal role-protected Administration route.

### 3. Role model and separation of duties

Roles are additive. Assign the minimum roles needed; do not use one unrestricted generic admin role.

| Role | Permitted responsibility | Explicitly excluded |
|---|---|---|
| `SystemAdministrator` | Application security settings, retention, audit access, emergency user recovery | Routine case processing and approval, unless separately assigned |
| `UserAdministrator` | Create, disable, invite, and assign ordinary users and scopes | System configuration, audit alteration, self-elevation |
| `ProcessingAdministrator` | Register, version, activate, and retire controlled templates and local processing rules | User administration and audit alteration |
| `Analyst` | Create assigned cases, upload source documents, start processing | Approve own output, administer templates/users |
| `Reviewer` | Review/correct processing rows and approve/export assigned cases | System/user administration |
| `Auditor` | Read-only audit, template, case, and output history within assigned scope | Modify data, download source documents by default |
| `ReadOnly` | View permitted case status and approved results | Upload, process, approve, export, or administer |

**Four-eyes rule:** an Analyst cannot approve or export a run they created or materially edited. A Reviewer must approve it. Initial export can require a Reviewer; optional dual approval for high-risk exports is a later policy setting.

### 4. Scope model

Role membership alone is insufficient. Access to operational data is constrained by assignment.

- A **Client** is the regulated customer/entity to which work relates.
- A **Case** is the bank-controlled unit of work under a Client.
- A **Case access grant** assigns a user or team one or more rights: `View`, `Upload`, `Process`, `Review`, `Approve`, `Export`.
- Every document, template use, processing run, correction, approval, and output belongs to one Case.
- Access checks run on the server for every API request. The frontend only reflects permissions; it does not enforce them.
- Default rule: deny access unless a role and an explicit scope grant allow it.

### 5. Template and output governance

- A template is never overwritten. Uploading a replacement creates a new immutable version.
- Only a `ProcessingAdministrator` can activate a new template version.
- A processing run records the exact template version, source-document versions, processing-rule version, initiator, and timestamps.
- Generated XLSX output is stored as a separate, immutable version with a SHA-256 hash.
- A correction or approval records the original value, revised value, reviewer, UTC timestamp, source evidence, and rationale where supplied.

### 6. Retention and deletion

Initial default retention policy, pending the bank's formal records schedule:

| Data class | Default | Operation |
|---|---:|---|
| Source documents | 7 years | Retain from case closure; purge only through an approved retention job |
| Generated outputs | 7 years | Retain with source case and export history |
| Templates and template versions | 7 years after retirement | Never overwrite; archive when retired |
| Audit records | 7 years minimum | Append-only; no application-level deletion |
| Temporary renderer/OCR files | Immediate deletion after processing | Must not enter backups or user-accessible storage |
| Disabled-user metadata | 7 years after disablement | Retain for accountability, subject to formal policy |

No user-facing hard delete is permitted for cases, source documents, outputs, templates, or audit records. The initial application exposes archive/request-for-purge workflows only. A legal-hold flag prevents retention jobs from purging related items.

**Bank action required before production:** Compliance/Records Management must approve or replace these defaults with the bank's applicable retention schedule and legal-hold process.

### 7. Audit requirements

Audit records are separate from diagnostic application logs. Audit records are append-only and normal application users cannot edit or delete them.

Record, at minimum:

- Login success, failure, lockout, logout, password reset, and session revocation.
- Account creation, disablement, reactivation, role/scope changes, and administrator actions.
- Access denied decisions.
- Case, document, template, processing-run, row-review, approval, archive, purge-request, and export events.
- Settings and security-policy changes.
- Audit-log access and export.

Each event contains: immutable event ID; UTC time; event type; actor ID; actor role snapshot; target type/ID; case/client scope where applicable; result; correlation ID; and safe before/after values for changes. Record IP/user-agent only where approved by bank policy.

Never write passwords, session values, access tokens, API keys, encryption keys, database connection strings, or raw source-document contents to audit logs.

### 8. Storage and data-protection controls

- Production SQL Server, application storage, and backups are bank controlled.
- Source documents and generated outputs are stored outside the web root, under opaque internal identifiers rather than user-provided filenames.
- File metadata records SHA-256 content hashes, upload actor, source filename, internal stored name, content type, byte size, and timestamps.
- Uploaded files must be allow-listed by type, size limited, malware scanned, and processed in an isolated working directory.
- Production uses TLS, encrypted storage/backups, least-privilege database/service accounts, and protected ASP.NET Core Data Protection keys.
- No API keys or application secrets belong in source control or frontend configuration.

## Current-system gap assessment

The current code has no application authentication or authorisation and therefore Phase 1 must protect every existing controller.

| Current component | Current condition | Phase 1 treatment |
|---|---|---|
| `SettingsController` | API/provider configuration is reachable without identity | SystemAdministrator policy only; later remove external-provider settings |
| `DocumentsController` | Documents are listed/changed without ownership checks; deletion is physical | Require authenticated case access; replace direct delete with archive/purge workflow |
| `ScreeningController` | Templates/runs lack actor, case, approval, and immutable-version metadata | Add case ownership, actor fields, template version controls, and reviewer approval |
| `ConversationsController` / chat data | No user ownership | Associate with Case and/or creator; restrict or retire when external AI is removed |
| `ProviderSettings` | Contains external provider configuration and encrypted API key | Keep temporarily behind SystemAdministrator; remove in local-only processing release |
| `storage/` | Shared file storage structure | Reorganise by opaque case/document/output IDs with dedicated permissions |

## Phase 1 implementation boundary

Phase 1 will deliver:

1. ASP.NET Core Identity schema and secure bootstrap of first System Administrator.
2. Login, logout, current-user, and server-side authentication middleware.
3. Default-deny authorization with roles/policies for every API endpoint.
4. Replacement of the demo-admin gate with role-protected routes.
5. Administrator user-management UI: create, activate, disable, roles, and scoped access.
6. Initial `Client`, `Case`, and `CaseAccessGrant` model, plus server-side checks on existing documents, templates, runs, and exports.
7. Append-only `AuditEvent` records for identity, access, configuration, document/template, processing, and export events.
8. Archive-first handling in place of destructive deletion.

Phase 1 will not introduce public registration, email delivery, external SSO, MFA, automatic retention purging, or AI/OCR changes.

## Database change strategy

All normal schema changes are implemented as Entity Framework Core migrations in the repository and applied by the application in development. Do not manually create Identity tables or alter production schema by copying ad-hoc SQL.

Expected new table groups for Phase 1:

- ASP.NET Core Identity tables (users, roles, claims, logins, tokens, user-role assignments).
- `Clients`, `Cases`, and `CaseAccessGrants`.
- `AuditEvents`.
- New foreign keys/metadata on documents, templates, runs, rows, and output records.
- Optional `FileArtifacts` table to centralise document/template/output hash and retention metadata.

Database changes must be tested against a restored anonymised copy or a dedicated staging database before production.

## Manual database actions required from the bank / DBA

No manual database query or schema change is required for Phase 0.

Before Phase 1 is deployed to a bank environment, the DBA must provide:

1. A dedicated SQL Server database (for example, `SummaryStudio`) and a non-`sa` application login.
2. A least-privilege application user permitted to read/write only this application database.
3. A separate migration/deployment login permitted to apply EF migrations; the runtime application login must not have schema-alter permissions after deployment.
4. SQL Server encryption-at-rest and backup settings in line with bank policy.
5. A tested backup and restore procedure.
6. Network rules allowing only the application server to connect to the SQL Server port.
7. A staging database with the same migration path as production.

For the current developer machine, the existing application is configured to create/update its local `SummarizerApp` database through EF Core migrations. Once the Windows Application Control issue is resolved, the normal developer command is:

```powershell
dotnet ef database update --project backend\Summarizer.Api\Summarizer.Api.csproj --startup-project backend\Summarizer.Api\Summarizer.Api.csproj
```

This command is only for the local development database. Do not run it directly against a bank production database without the approved migration/deployment process.

## Phase 1 acceptance criteria

Phase 1 is accepted only when:

- Unauthenticated users cannot access any operational or administrative API.
- The old shared demo-admin password cannot grant access.
- A normal user is sent to Processing and cannot invoke administrative APIs or routes.
- Every privileged action is checked server-side and logged.
- An Analyst cannot approve/export their own run.
- Documents, templates, runs, and exports are linked to a Case and access is scope checked.
- Application users cannot alter or delete audit events.
- No hard-delete endpoint remains for governed records.
- Migration, backup/restore, permission, and audit tests pass in staging.

## Phase 1 local setup (identity foundation)

The first implementation slice adds local ASP.NET Core Identity, cookie login/logout, a controlled bootstrap administrator, default-deny API authentication, role policies, and identity audit events.

### One-time developer database migration

`dotnet-ef` is not installed in this workspace. Install the official EF CLI once for the developer account, then create and apply the migration from the repository root:

```powershell
dotnet tool install --global dotnet-ef --version 9.0.5
dotnet ef migrations add AddIdentityAndAudit --project backend\Summarizer.Api\Summarizer.Api.csproj --startup-project backend\Summarizer.Api\Summarizer.Api.csproj
dotnet ef database update --project backend\Summarizer.Api\Summarizer.Api.csproj --startup-project backend\Summarizer.Api\Summarizer.Api.csproj
```

The generated migration must be committed with the code. Do not hand-create the `AspNet*` or `AuditEvents` tables with SQL.

### First administrator

Before the first application launch after the migration, set the bootstrap values only in the active PowerShell session. Use a unique password of at least 14 characters with upper-case, lower-case, digit, and symbol characters.

```powershell
$env:BootstrapAdmin__UserName = 'initial-admin'
$env:BootstrapAdmin__Email = 'initial-admin@bank.example'
$env:BootstrapAdmin__Password = 'replace-with-a-unique-14-plus-character-password'
dotnet run --project backend\Summarizer.Api\Summarizer.Api.csproj --launch-profile http
```

The application creates the first `SystemAdministrator` only if that username does not already exist. After first successful login, remove the password from the session:

```powershell
Remove-Item Env:BootstrapAdmin__Password
```

For a bank deployment, inject all three bootstrap values through the bank's deployment secret store, create the administrator, and immediately remove the bootstrap password. Never commit them to `appsettings*.json`, a `.env` file, or Git.

## Phase 2 local setup (client and case scope)

The client/case foundation introduces the `Clients`, `Cases`, and `CaseAccessGrants` tables. Before testing it, create and apply a new EF migration after stopping the API:

```powershell
dotnet ef migrations add AddClientCaseAccess --project backend\Summarizer.Api\Summarizer.Api.csproj --startup-project backend\Summarizer.Api\Summarizer.Api.csproj
dotnet ef database update --project backend\Summarizer.Api\Summarizer.Api.csproj --startup-project backend\Summarizer.Api\Summarizer.Api.csproj
```

Do not create these tables manually. The first scope slice provides protected APIs and audit events; the next UI slice will require analysts to select an assigned case before they upload documents or start a screening run.
