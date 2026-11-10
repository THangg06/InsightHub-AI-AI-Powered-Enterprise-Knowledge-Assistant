# Architecture

The application has three layers:

1. **React client**: accepts a workspace name, uploads text files, sends questions, and renders answers with citations.
2. **ASP.NET Core API**: validates request shape, enforces the demo workspace token, limits files to 2 MB, and exposes document and question endpoints.
3. **Application services**: `WorkspaceStore` owns in-memory documents and `RetrievalService` ranks documents by keyword overlap.

The current implementation uses in-memory storage and deterministic retrieval so it runs without credentials. A production deployment would replace these services with a database, chunking/indexing, embedding generation, vector search, and an LLM gateway.

