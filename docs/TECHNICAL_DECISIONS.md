# Technical decisions and failure handling

## Provider-free baseline

**Problem:** provider credentials make local onboarding and CI fragile. **Options:** require an LLM, mock all behavior, or implement deterministic retrieval. **Choice:** deterministic retrieval with citations. It is transparent and testable, but it is not semantic search and does not generate natural-language synthesis. A future vector index and LLM gateway can replace the service behind the same endpoint.

## Failure scenarios

| Scenario | Detection | Current handling | Recovery or improvement |
|---|---|---|---|
| Embedding API unavailable | Not applicable in baseline | No external embedding call is made | Add timeout, retry policy, and a degraded keyword mode |
| LLM timeout | Not applicable in baseline | No LLM call is made | Use a cancellation token, bounded timeout, and user-visible retry |
| Empty document | Missing or whitespace content | HTTP 400 with a clear error | Keep rejecting invalid source data |
| Large upload | File exceeds 2 MB | HTTP 400 before storage | Add streaming upload and configurable object storage limits |
| Irrelevant retrieval | No document shares question terms | Answer says no answer was found and returns no citations | Add semantic retrieval and a confidence threshold |
| Duplicate document | Not detected in baseline | Duplicate content can be stored | Hash normalized content and make insertion idempotent |
| Unauthorized workspace | Token header does not equal workspace token | HTTP 401 and no document access | Replace demo token with authenticated user/workspace claims |
| Database connection failure | Not applicable because storage is in memory | Process remains independent of a database | Add health checks, bounded retries, and an operator alert |

