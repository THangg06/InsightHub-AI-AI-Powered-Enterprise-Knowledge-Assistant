# Test and Technical Audit Report

## Audit scope

This audit covers the repository currently available at `D:\CNTT\RAG_NET`. The project has now been expanded toward the supplied full-stack requirements. External provider and Docker execution results remain environment-dependent.

## Phase 0: detected structure

- `src/RagWorkspace.Api`: ASP.NET Core API targeting .NET 8 with Identity, JWT, EF Core, extraction, chunking, and retrieval.
- `tests/RagWorkspace.Api.Tests`: xUnit unit and API integration tests.
- `web`: React, TypeScript, and Vite frontend.
- `.github/workflows/ci.yml`: backend and frontend CI jobs.
- `docs`: architecture, workflow, AI workflow, CI/CD, decisions, interview, and this report.

## Technology actually detected

| Area | Detected implementation |
|---|---|
| Backend | ASP.NET Core Web API, target framework `net8.0` |
| Storage | EF Core InMemory by default; PostgreSQL/pgvector in Docker |
| Retrieval | 384-dimensional deterministic embeddings and cosine similarity |
| AI | Embedding interface with deterministic local implementation; no live LLM |
| Authentication | ASP.NET Identity password hashing and JWT |
| Frontend | React 18, TypeScript, Vite |
| Tests | xUnit backend unit tests |
| Infrastructure | GitHub Actions only; no Docker or Compose files |

## Environment inspection

| Tool or service | Result |
|---|---|
| Node.js | Available: `v24.15.0` |
| npm | Available: `11.12.1` |
| .NET SDK | Installed during audit: `8.0.427` |
| Docker CLI | Missing; `docker --version` cannot run |
| PostgreSQL service | Not available for this baseline |

## Initial status

- Frontend dependency installation and `npm run build` passed.
- Backend restore and build passed after installing .NET SDK `8.0.427`.
- A later final test run passed 8/8 after the EF migration and citation changes.
- No remote GitHub operation was performed.
- The Git working tree was clean after the initial scaffold commits.

## Follow-up fixes applied

- CORS now defaults to the local Vite origin and can be configured through `Cors:AllowedOrigins`.
- The upload endpoint accepts only `.txt`, `.md`, and `.csv` extensions and stores a basename.
- The question endpoint returns a validation error for a missing JSON body.
- The frontend encodes workspace route values and reports API/network errors visibly.
- A workspace authorization unit test was added.
- Document endpoints now return metadata DTOs rather than full document content.
- API integration tests cover unauthorized access, upload/retrieval, and unsupported extensions.
- Retrieval stop-word filtering fixed a false-positive citation and all backend tests now pass.

## Initial risks and gaps

1. The demo authorization token equals the workspace ID. This is predictable and is not suitable for production authentication.
2. Upload validation checks size and extension, but not content type or content signatures.
3. Retrieval uses deterministic local embeddings and an extractive fallback; Gemini synthesis is optional and requires a configured key.
4. In-memory storage loses all documents when the process stops and does not provide duplicate detection.
5. There is no Docker, PostgreSQL, EF Core, Identity/JWT, Gemini, embedding, or Azure implementation in this repository.
6. Development dependency audit was not used as a release gate; production dependency audit reported zero vulnerabilities.

## Testing plan

1. Install .NET 8 SDK and run `dotnet restore`, `dotnet build`, and `dotnet test`.
2. Add API-level tests for authorization, empty and oversized uploads, malformed requests, and workspace isolation.
3. Run `npm ci` and `npm run build`.
4. Add browser-level checks for upload, question, citation rendering, and API error states.
5. If persistent infrastructure is introduced, run migration, connection failure, and data-isolation tests against an isolated database.
6. If external AI providers are introduced, test timeouts, unavailable-provider fallback, prompt boundaries, and citation grounding without exposing credentials.

## Phase results

### 1. Executive summary

The application now has a substantial local full-stack implementation: JWT authentication, EF Core entities/migration, PostgreSQL/pgvector Docker configuration, PDF/DOCX/TXT extraction, chunking, embeddings, persisted citations, and a React auth/workspace/chat flow. Backend build and frontend build pass. The live PostgreSQL, Docker, Gemini, Azure, and final post-migration test execution remain environment-dependent.

### 2. Backend

- **PASS:** `dotnet restore RagWorkspace.sln`.
- **PASS:** `dotnet build RagWorkspace.sln --configuration Release` with 0 warnings and 0 errors.
- **PASS:** final backend test run passed 8/8.
- **NOT TESTED:** live PostgreSQL integration and pgvector query execution because Docker/PostgreSQL are unavailable.

### 3. Frontend

- **PASS:** `npm run build`.
- **PASS:** `npm audit --omit=dev` reported 0 vulnerabilities.
- **NOT TESTED:** browser automation, because no browser automation tool was available in this execution.
- **PASS:** authentication screens, protected API routes, workspace creation, document list, and citation rendering are implemented.

### 4. Database and persistence

- **PASS:** EF Core entities, relationships, unique document hash, cascade relationships, vector(384) mapping, and `InitialCreate` migration generated.
- **NOT TESTED:** applying the migration against a live PostgreSQL instance and executing a pgvector index/query.
- No destructive database operation was performed.

### 5. Authentication and authorization

- **PASS:** registration, password hashing through Identity, login JWT issuance, JWT validation, and workspace membership checks are implemented.
- **NOT TESTED:** expired token behavior against a live deployed service.

### 6. Document processing

- **PASS:** PDF/DOCX/TXT/MD/CSV extraction, empty content rejection, 10 MB limit, basename handling, chunk persistence, embeddings, and duplicate hash policy are implemented.
- **NOT TESTED:** corrupted PDF/DOCX cases and production storage retry behavior.

### 7. RAG

- **PASS:** actual uploaded text is extracted, chunked, embedded, retrieved within a workspace, and returned with a source document ID.
- **PASS:** ten-case evaluation dataset documented in [RAG_EVALUATION.md](RAG_EVALUATION.md).
- **NOT IMPLEMENTED:** Gemini answer synthesis, hosted embedding provider, provider retries/timeouts, and true semantic model quality evaluation.
- No accuracy percentage is claimed.

### 8. Chat API

- **PARTIAL:** assistant chat messages and citations are persisted per question; full session listing/history/delete endpoints and LLM failure states remain.

### 9. Security

- **FIXED:** document content exposure in upload/list responses.
- **FIXED:** wildcard CORS and predictable token-only authorization.
- **REMAINING MEDIUM:** no rate limiting, audit logging, prompt-injection defense, or hosted-provider secret rotation.

### 10. Docker and cloud

- **PASS:** Dockerfiles and Compose configuration are present.
- **BLOCKED:** Docker build/start and Azure deployment because Docker CLI and cloud credentials are unavailable.
- Docker CLI was not installed, so Docker commands were not executed.

### 11. Performance and reliability

- **PASS:** upload is bounded at 2 MB; retrieval returns at most three matches.
- **NOT MEASURED:** latency, memory, concurrency, database performance, embedding batch size, and LLM latency.

## Issues fixed

| Priority | Issue | Root cause | Verification |
|---|---|---|---|
| P1 | Document body exposed in API responses | Domain record was returned directly | Integration test asserts body is absent; suite passes |
| P2 | CORS allowed every origin | `AllowAnyOrigin()` default | Configuration review and frontend build |
| P2 | False-positive retrieval citation | Stop words counted as evidence | Regression test and suite pass |
| P2 | Unsupported file types accepted | No extension allowlist | Integration test returns 400 |
| P2 | Frontend hid network/API failures | Fetch errors were not handled | TypeScript/Vite build passes |
| P3 | Missing backend test namespace | xUnit global using absent | Build and test suite pass |

## Remaining issues

The project is not the full InsightHub AI stack requested by the supplied prompt. Implementing the missing production features is a separate scope requiring architecture decisions, provider credentials, PostgreSQL/pgvector infrastructure, authentication design, and additional tests. Those features must not be represented as verified by this audit.

## Final status

- **PASS:** backend build, eight backend tests, JWT/upload/chat HTTP smoke test, frontend build, production dependency audit.
- **BLOCKED:** Docker/cloud/database phases because the implementations and Docker CLI are absent.
- **PARTIAL:** Gemini/hosted embeddings, full chat history API, Azure, and live Docker/PostgreSQL remain unverified or incomplete.
- **NOT TESTED:** browser automation and production-scale performance.

## How to run

```powershell
cd D:\CNTT\RAG_NET
dotnet restore RagWorkspace.sln
dotnet build RagWorkspace.sln --configuration Release
dotnet test RagWorkspace.sln --configuration Release
dotnet run --project src\RagWorkspace.Api
```

In another terminal:

```powershell
cd D:\CNTT\RAG_NET\web
npm ci
npm run build
npm run dev
```

Use workspace `demo`, upload a `.txt`, `.md`, or `.csv` file, then ask a question supported by its text. The current API stores data in memory and loses it on restart.

## Verification boundary

This report distinguishes executed checks from unavailable checks. No backend or external-service test is marked as passed until it has actually run in an environment containing the required SDK or service.
