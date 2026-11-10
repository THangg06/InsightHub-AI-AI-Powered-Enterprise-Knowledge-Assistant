# Technical decisions and failure handling

## Provider-free baseline

**Problem:** provider credentials make local onboarding and CI fragile. **Options:** require an LLM, mock all behavior, or implement a deterministic local embedding adapter with an optional LLM gateway. **Choice:** deterministic 384-dimensional embeddings with cosine retrieval and optional Gemini synthesis. It is reproducible and testable, while the gateway can provide natural-language synthesis when configured. Hosted provider quality and availability remain environment-dependent.

## Failure scenarios

| Scenario | Detection | Current handling | Recovery or improvement |
|---|---|---|---|
| Embedding API unavailable | Hosted embedding is not enabled by default | Local deterministic adapter remains available | Add timeout, retry policy, and a degraded local mode |
| LLM timeout | Gemini request exceeds the 30-second client timeout | API returns a gateway failure rather than a false answer | Add bounded retry and user-visible retry |
| Empty document | Missing or whitespace content | HTTP 400 with a clear error | Keep rejecting invalid source data |
| Large upload | File exceeds 2 MB | HTTP 400 before storage | Add streaming upload and configurable object storage limits |
| Irrelevant retrieval | No document shares question terms | Answer says no answer was found and returns no citations | Add semantic retrieval and a confidence threshold |
| Duplicate document | Not detected in baseline | Duplicate content can be stored | Hash normalized content and make insertion idempotent |
| Unauthorized workspace | JWT user is not a workspace member | HTTP 401 and no document access | Add role-based membership policies and audit events |
| Database connection failure | PostgreSQL provider cannot connect during startup | Process fails clearly instead of silently using the wrong store | Add health checks, bounded retries, and an operator alert |
