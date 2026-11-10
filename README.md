# RAG Workspace

RAG Workspace is a small full-stack demonstration of document upload, workspace isolation, retrieval, and cited question answering. It is designed to show practical engineering decisions for a Full Stack Intern interview.

## Status

The current vertical slice is intentionally provider-free:

- ASP.NET Core .NET 8 Web API with in-memory storage.
- React + TypeScript frontend built with Vite.
- Keyword retrieval over uploaded plain-text documents.
- Answers include document citations and explicit "not found" behavior.
- Uploads accept `.txt`, `.md`, and `.csv` files up to 2 MB.
- Unit tests cover retrieval and workspace authorization.

Production embedding providers, persistent storage, authentication, and a hosted LLM are documented as future improvements rather than claimed features.

## Run locally

### Backend

Install the .NET 8 SDK, then:

```powershell
dotnet restore
dotnet run --project src/RagWorkspace.Api
```

The API listens on `http://localhost:5000`.

### Frontend

Install Node.js 20+, then:

```powershell
cd web
npm install
npm run dev
```

The Vite development server proxies `/api` requests to the API.

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
