# API authorization map

The API requires an authenticated session by default. Public exceptions are login and health. Case grants are checked on the server for individual records; the frontend's role and permission display is advisory only.

| Endpoint group | Required role | Required case grant |
| --- | --- | --- |
| Conversations: list/get | Any authenticated user | View (system administrator sees all) |
| Conversations: create/archive/restore | SystemAdministrator, ProcessingAdministrator, Analyst | Upload |
| Documents: list | Any authenticated user | View (system administrator sees all) |
| Documents: upload/archive/restore | SystemAdministrator, ProcessingAdministrator, Analyst | Upload |
| Documents: reprocess | SystemAdministrator, ProcessingAdministrator, Analyst | Process |
| Generic analysis, if enabled | SystemAdministrator, ProcessingAdministrator, Analyst | Process on an existing, active case conversation |
| Screening templates: list | Any authenticated user | None; templates are shared configuration |
| Screening templates: register | SystemAdministrator, ProcessingAdministrator | None |
| Screening runs: create | SystemAdministrator, ProcessingAdministrator, Analyst | Process |
| Screening runs: list | Any authenticated user | View on the requested case |
| Screening runs: get | Any authenticated user | View |
| Screening rows: update | SystemAdministrator, ProcessingAdministrator, Analyst, Reviewer | Review |
| Screening runs: download XLSX | SystemAdministrator, ProcessingAdministrator, Analyst | Process |
| Clients: list/create; cases: create; case grants: change | SystemAdministrator, ProcessingAdministrator | Approve for grant changes |
| Cases: list | Any authenticated user | Any case grant (system administrator sees all) |
| Users: list/create/roles/disable | SystemAdministrator, UserAdministrator | None |
| Users: active-user picker | SystemAdministrator, ProcessingAdministrator, Analyst | None |
| Settings and legacy assignments | SystemAdministrator | None |
| Entity mappings | SystemAdministrator, ProcessingAdministrator | None |
| Audit history | SystemAdministrator, Auditor | None |

Role policies and case grants are both required where both columns apply. Grant strings are parsed as comma-separated exact permission names. Older unassigned records remain accessible only through the system administrator legacy-assignment flow.
New case grants include `View` automatically so users assigned another case action can see its workspace and processing runs.

Screening runs generate one template-row set for each uploaded source file. Download is available immediately after processing; missing values remain marked for review. The maker-tracking fields remain in the database for historical runs, but approval is no longer part of the current processing workflow.
