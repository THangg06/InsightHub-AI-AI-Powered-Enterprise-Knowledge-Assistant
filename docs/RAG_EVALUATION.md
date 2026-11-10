# RAG evaluation

## Scope

The current local implementation extracts PDF/DOCX/TXT content, chunks it, creates deterministic 384-dimensional embeddings, and retrieves chunks by cosine similarity. Gemini answer generation is optional and requires a configured API key. The dataset below evaluates local retrieval and citation behavior; it is not a claim of live Gemini quality.

## Evaluation dataset

The source document used for the cases is:

> `platform.txt`: “The platform uses GitHub Actions for continuous integration. Documents are limited to 2 MB and may be uploaded as TXT, Markdown, or CSV. The demo stores documents in memory. Questions with no matching evidence receive a not-found response.”

| # | Question | Expected evidence | Expected result |
|---:|---|---|---|
| 1 | What runs continuous integration? | GitHub Actions | Citation returned |
| 2 | What is the maximum document size? | 2 MB | Citation returned |
| 3 | Which file formats are accepted? | TXT, Markdown, CSV | Citation returned |
| 4 | Where are documents stored? | In memory | Citation returned |
| 5 | What happens without evidence? | Not-found response | No citation |
| 6 | How does CI run? | GitHub Actions | Citation returned |
| 7 | Is a 5 MB upload supported? | 2 MB limit | Citation returned |
| 8 | Does the application use PostgreSQL? | No supporting evidence | No citation |
| 9 | What is the production embedding model? | No supporting evidence | No citation |
| 10 | Tài liệu được lưu ở đâu? | In memory (Vietnamese query) | Result depends on shared keyword overlap; semantic Vietnamese retrieval is not supported |

## Execution status

The frontend build and static checks have been executed. Backend evaluation is **BLOCKED** until the .NET 8 SDK is available in the execution environment. No retrieval accuracy percentage is claimed.

When the SDK is available, execute:

```powershell
dotnet test RagWorkspace.sln --configuration Release
```

The integration test verifies that uploaded content is used for a cited answer and that the upload response does not expose the full document body.

## Future semantic-RAG evaluation

After adding a real extractor, chunker, embedding provider, and vector database, this dataset should be extended with source page/chunk identifiers and measured for retrieval recall, citation precision, answer correctness, abstention correctness, latency, and provider error recovery. Results should be recorded from a repeatable test run rather than estimated manually.
