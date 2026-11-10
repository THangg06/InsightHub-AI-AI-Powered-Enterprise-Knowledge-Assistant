# AI-assisted development workflow

AI assistance is used as a reviewable tool, not as an authority. Every suggestion is checked against requirements, project conventions, tests, and security considerations.

## Requirement analysis

- **Original task:** translate the Hitachi Full Stack Intern requirements into repository deliverables.
- **Prompt used:** “Map these requirements to an implementable full-stack project with explicit limitations.”
- **Suggestion:** group work into Git, backend, frontend, CI, and documentation.
- **Review and modification:** kept only features that can be run without secret provider credentials.
- **Verification:** checked every requirement against the repository file list.
- **Final result:** a traceable scaffold and requirement-specific documents.

## Code generation and architecture

- **Original task:** build a minimal document upload and cited-question vertical slice.
- **Prompt used:** “Create a provider-free ASP.NET Core API and React client with workspace authorization and deterministic retrieval.”
- **Suggestion:** use in-memory storage and keyword overlap for a deterministic demo.
- **Review and modification:** added size and empty-file validation, explicit unauthorized responses, and a no-result response.
- **Verification:** backend tests cover citations and irrelevant questions; CI contains build and test commands.
- **Final result:** a runnable baseline that does not pretend to have a hosted LLM.

## Code review, tests, debugging, refactoring, and documentation

For each change, AI-generated code is reviewed for input validation, authorization boundaries, null handling, and maintainability. Tests are generated from observable behavior, then adjusted to assert the actual API contract. Debugging follows reproduction, a focused fix, and a regression test. Refactoring is only accepted when behavior remains covered. Documentation is written after implementation so it describes verified behavior rather than aspirations.

