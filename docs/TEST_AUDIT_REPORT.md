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
| .NET SDK | Installed during audit: `8.0.427` |
| Docker CLI | Missing; `docker --version` cannot run |
| PostgreSQL service | Not available for this baseline |

## Initial status

- Frontend dependency installation and `npm run build` passed.
- Backend restore, build, and tests were executed after installing .NET SDK `8.0.427`.
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
3. Retrieval returns a cited relevance summary rather than an LLM-synthesized answer. This is intentional for the provider-free baseline and must not be presented as a production RAG pipeline.
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

The implemented provider-free vertical slice is buildable and testable. Backend build and all six tests pass; frontend build and production dependency audit pass. The supplied InsightHub requirements for PostgreSQL, pgvector, Identity/JWT, PDF/DOCX extraction, embeddings, Gemini, chat persistence, Docker, and Azure remain unimplemented and are explicitly not claimed.

### 2. Backend

- **PASS:** `dotnet restore RagWorkspace.sln`.
- **PASS:** `dotnet build RagWorkspace.sln --configuration Release` with 0 warnings and 0 errors.
- **PASS:** `dotnet test RagWorkspace.sln --configuration Release`: 6 passed, 0 failed.
- **PASS:** live HTTP smoke test: `/api/health` returned 200; upload returned 201; question returned 200 with a real citation; missing token returned 401.

### 3. Frontend

- **PASS:** `npm run build`.
- **PASS:** `npm audit --omit=dev` reported 0 vulnerabilities.
- **NOT TESTED:** browser automation, because no browser automation tool was available in this execution.
- **NOT APPLICABLE:** authentication screens, dashboard statistics, protected routes, and chat history are not implemented.

### 4. Database and persistence

- **NOT IMPLEMENTED:** PostgreSQL, EF Core, migrations, relationships, pgvector, vector dimensions, and persistence.
- **NOT TESTED:** database connection, migrations, cleanup, and vector similarity queries.
- No destructive database operation was performed.

### 5. Authentication and authorization

- **PASS:** demo token mismatch is rejected by unit and integration tests.
- **REMAINING RISK:** token equality with workspace ID is predictable and is not production authentication.
- **NOT IMPLEMENTED:** Identity, password hashing, JWT signature/expiry, user registration, membership claims, and cross-user workspace authorization.

### 6. Document processing

- **PASS:** supported extension, empty content, size limit, and basename handling are implemented and covered where applicable.
- **NOT IMPLEMENTED:** PDF/DOCX extraction, page metadata, chunk persistence, processing statuses, retries, duplicate policy, and storage cleanup.

### 7. RAG

- **PASS:** actual uploaded text is retrieved, excerpted, and returned with the actual document ID and filename.
- **PASS:** ten-case evaluation dataset documented in [RAG_EVALUATION.md](RAG_EVALUATION.md).
- **NOT IMPLEMENTED:** semantic chunking, embeddings, pgvector, LLM context, Gemini, provider retries/timeouts, and semantic Vietnamese retrieval.
- No accuracy percentage is claimed.

### 8. Chat API

- **NOT IMPLEMENTED:** sessions, history, messages, deletion, persistence, concurrent request policy, and AI-provider failure states.

### 9. Security

- **FIXED:** document content exposure in upload/list responses.
- **FIXED:** wildcard CORS.
- **FIXED:** path-like filenames are reduced to a basename.
- **REMAINING MEDIUM:** predictable demo authorization token.
- **REMAINING MEDIUM:** no rate limiting, durable identity, audit logging, or prompt-injection defense because those production features are absent.

### 10. Docker and cloud

- **NOT IMPLEMENTED/BLOCKED:** Docker Compose, container build, PostgreSQL service, pgvector, Blob Storage, and Azure deployment.
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

- **PASS:** provider-free backend build, six backend tests, HTTP smoke test, frontend build, production dependency audit.
- **BLOCKED:** Docker/cloud/database phases because the implementations and Docker CLI are absent.
- **NOT IMPLEMENTED:** Identity/JWT, PDF/DOCX, embeddings, pgvector, Gemini, persistent chat, and Azure.
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
