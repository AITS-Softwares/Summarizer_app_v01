# Summary Studio

AITS-branded document analysis workspace built with React, ASP.NET Core, and SQL Server.

## Structure

- `frontend/` - React 19, Vite, TypeScript, Tailwind CSS
- `backend/Summarizer.Api/` - ASP.NET Core 9 Web API and EF Core
- `database/` - SQL Server setup scripts
- `storage/` - uploaded documents and data-protection keys; ignored by Git

## Implemented

- SQL-backed conversations, messages, documents, reports, and provider settings
- Multi-file and folder selection with server upload
- OpenAI-compatible API and local Ollama provider modes
- Encrypted API-key persistence through ASP.NET Core Data Protection
- Document library, saved reports, search, archive, restore, and delete actions
- Guided provider/model presets and a styled model picker in the chat composer
- Visible file/folder attachment tray with relative folder paths
- Stop generation, edit-and-regenerate prompts, speech-to-text, and collapsible Sources
- Swagger, health checks, IIS configuration, and Docker Compose

Text extraction currently supports text, Markdown, CSV, JSON, XML, HTML, source code, SQL, YAML, and log files. Other formats are stored and tracked; PDF, Office, OCR, and image extraction are the next processing phase.

## Local development

Start SQL Server LocalDB:

```powershell
SqlLocalDB.exe start MSSQLLocalDB
```

Start the API:

```powershell
dotnet run --project backend\Summarizer.Api\Summarizer.Api.csproj --launch-profile http
```

Start the frontend in a second terminal:

```powershell
cd frontend
npm.cmd install
npm.cmd run dev
```

Open `http://localhost:5173`. Swagger is available at `http://localhost:5036/swagger`.

## Database migrations

```powershell
dotnet ef database update --project backend\Summarizer.Api\Summarizer.Api.csproj --startup-project backend\Summarizer.Api\Summarizer.Api.csproj
```

## IIS deployment

1. Publish the API:

```powershell
dotnet publish backend\Summarizer.Api\Summarizer.Api.csproj -c Release -o publish\api
```

2. Build the frontend:

```powershell
cd frontend
npm.cmd run build
```

3. Create separate IIS applications for `publish/api` and `frontend/dist`.
4. Install the ASP.NET Core Hosting Bundle and IIS URL Rewrite module.
5. Set the production SQL Server connection string in the API environment or `appsettings.Production.json`.
6. Give the API application-pool identity write access to the configured `storage` folders.
7. Set `VITE_API_URL` before the frontend build when the API uses a different origin.

## Docker

```powershell
docker compose up --build
```

Open `http://localhost:8080`. Override `MSSQL_SA_PASSWORD` outside development.
