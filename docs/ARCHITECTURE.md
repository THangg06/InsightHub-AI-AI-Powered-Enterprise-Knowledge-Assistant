# Architecture

The application has three layers:

1. **React client**: authenticates users, creates workspaces, uploads supported documents, and renders cited answers.
2. **ASP.NET Core API**: exposes authentication, workspace, document, and question endpoints with JWT and membership checks.
3. **Application services**: extraction, chunking, deterministic embeddings, and cosine retrieval are isolated behind services.
4. **PostgreSQL/pgvector**: EF Core stores users, memberships, documents, chunks, embeddings, chat messages, and citations.

The default development configuration uses EF Core InMemory so the project can run without Docker. Docker Compose switches to PostgreSQL with the pgvector extension and applies the initial migration. The embedding interface can be replaced by a hosted provider without changing the upload or retrieval endpoints.

CORS defaults to the local Vite origin and is configured in `src/RagWorkspace.Api/appsettings.json`. Production deployments must provide a strong JWT key and external secret management.
