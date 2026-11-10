# Test and Technical Audit Report

## Audit scope

This audit covers the repository currently available at `D:\CNTT\RAG_NET`. It does not claim that this repository is the separate “InsightHub AI” stack described in the supplied master prompt. The detected project is a smaller provider-free ASP.NET Core and React demonstration.

## Phase 0: detected structure

- `src/RagWorkspace.Api`: ASP.NET Core minimal API targeting .NET 8.
- `tests/RagWorkspace.Api.Tests`: xUnit tests for deterministic retrieval.
- `web`: React, TypeScript, and Vite frontend.
- `.github/workflows/ci.yml`: backend and frontend CI jobs.
- `docs`: architecture, workflow, AI workflow, CI/CD, decisions, interview, and this report.

## Technology actually detected

| Area | Detected implementation |
|---|---|
| Backend | ASP.NET Core Web API, target framework `net8.0` |
| Storage | In-memory `ConcurrentDictionary`; no EF Core or PostgreSQL |
| Retrieval | Case-insensitive keyword overlap; no embeddings or pgvector |
| AI | No external LLM or embedding provider |
| Authentication | Demo `X-Workspace-Token` comparison; no Identity or JWT |
| Frontend | React 18, TypeScript, Vite |
| Tests | xUnit backend unit tests |
| Infrastructure | GitHub Actions only; no Docker or Compose files |

## Environment inspection

| Tool or service | Result |
|---|---|
| Node.js | Available: `v24.15.0` |
| npm | Available: `11.12.1` |
| .NET SDK | Missing; `dotnet --info` cannot run |
| Docker CLI | Missing; `docker --version` cannot run |
| PostgreSQL service | Not available for this baseline |

## Initial status

- Frontend dependency installation and `npm run build` passed.
- Backend restore, build, and tests could not be executed because the .NET SDK is not installed.
- No remote GitHub operation was performed.
- The Git working tree was clean after the initial scaffold commits.

## Follow-up fixes applied

- CORS now defaults to the local Vite origin and can be configured through `Cors:AllowedOrigins`.
- The upload endpoint accepts only `.txt`, `.md`, and `.csv` extensions and stores a basename.
- The question endpoint returns a validation error for a missing JSON body.
- The frontend encodes workspace route values and reports API/network errors visibly.
- A workspace authorization unit test was added.

## Initial risks and gaps

1. The demo authorization token equals the workspace ID. This is predictable and is not suitable for production authentication.
2. CORS allows every origin. This is convenient for local development but unsafe as a production default.
3. Upload validation checks size and content but does not restrict accepted extensions or content types.
4. The question endpoint should explicitly handle a missing or malformed JSON body.
5. Retrieval returns a cited relevance summary rather than an LLM-synthesized answer. This is intentional for the provider-free baseline and must not be presented as a production RAG pipeline.
6. In-memory storage loses all documents when the process stops and does not provide duplicate detection.
7. There is no Docker, PostgreSQL, EF Core, Identity/JWT, Gemini, embedding, or Azure implementation in this repository.
8. The first frontend dependency installation reported npm audit findings; these need review separately from the functional build.

## Testing plan

1. Install .NET 8 SDK and run `dotnet restore`, `dotnet build`, and `dotnet test`.
2. Add API-level tests for authorization, empty and oversized uploads, malformed requests, and workspace isolation.
3. Run `npm ci` and `npm run build`.
4. Add browser-level checks for upload, question, citation rendering, and API error states.
5. If persistent infrastructure is introduced, run migration, connection failure, and data-isolation tests against an isolated database.
6. If external AI providers are introduced, test timeouts, unavailable-provider fallback, prompt boundaries, and citation grounding without exposing credentials.

## Verification boundary

This report distinguishes executed checks from unavailable checks. No backend or external-service test is marked as passed until it has actually run in an environment containing the required SDK or service.
