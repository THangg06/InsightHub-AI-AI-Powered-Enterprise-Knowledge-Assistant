# Development workflow

1. Create a feature branch from `main`, for example `feature/document-upload`.
2. Make one focused change and add tests or documentation with it.
3. Run `dotnet test RagWorkspace.sln` and `npm run build` in `web`.
4. Review the diff for secrets and unrelated changes.
5. Commit with a descriptive message such as `feat: add cited document questions`.
6. Open a pull request and complete the checklist in `.github/pull_request_template.md`.
7. Merge only after CI passes and review comments are resolved.

This repository was initialized locally for this scaffold. No remote repository, branch push, or pull request is claimed until a developer performs that operation.

