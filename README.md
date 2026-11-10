# RAG Workspace

RAG Workspace is a full-stack document assistant with JWT authentication, workspace isolation, persistent PostgreSQL/pgvector storage, document extraction, chunking, vector-style retrieval, and cited answers.

## Status

The local application includes:

- ASP.NET Core .NET 8 Web API with ASP.NET Identity and JWT.
- PostgreSQL/pgvector schema through EF Core migrations.
- PDF, DOCX, TXT, Markdown, and CSV extraction.
- Chunking and deterministic 384-dimensional embeddings for local reproducibility.
- Workspace-scoped retrieval and persisted citation records.
- React + TypeScript frontend with register, login, workspace, upload, and chat flows.

The deterministic embedding provider is intentionally replaceable. A hosted Gemini/embedding provider still requires credentials and is not claimed as live-verified.

## Run locally

### Backend

Install the .NET 8 SDK, then use the in-memory development default:

```powershell
dotnet restore
dotnet run --project src/RagWorkspace.Api
```

The API listens on `http://localhost:5000`.

For the full PostgreSQL/pgvector stack:

```powershell
docker compose up --build
```

This starts PostgreSQL on port 5432, the API on port 5000, and the frontend on port 5173. Change `Jwt__Key` in a real deployment; never use the compose example secret in production.

### Frontend

Install Node.js 20+, then:

```powershell
cd web
npm install
npm run dev
```

The Vite development server proxies `/api` requests to the API. Register an account, create a workspace, upload a supported document, and ask a question.

The API allows the local Vite origin by default. Configure additional origins through `Cors:AllowedOrigins` in `src/RagWorkspace.Api/appsettings.json`.

## Repository guide

- [Architecture](docs/ARCHITECTURE.md)
- [Development workflow](docs/DEVELOPMENT_WORKFLOW.md)
- [AI-assisted workflow](docs/AI_WORKFLOW.md)
- [CI/CD](docs/CI_CD.md)
- [Technical decisions](docs/TECHNICAL_DECISIONS.md)
- [Interview guide](docs/INTERVIEW_GUIDE.md)
- [Pull request checklist](.github/pull_request_template.md)

## Security

Never commit API keys, passwords, access tokens, or local `.env` files. Copy `.env.example` only when local configuration is needed.
