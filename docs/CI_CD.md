# CI/CD

GitHub Actions runs on pushes to `main` and pull requests.

- The backend job installs .NET 8, restores the solution, builds in Release mode, and runs unit tests.
- The frontend job installs Node.js 20, runs `npm ci`, and builds the Vite application.
- A failed command fails its job and is visible in the Actions summary.
- The backend project contains an EF Core `InitialCreate` migration. Docker Compose validation is available locally when Docker is installed.

There is no automatic Azure deployment yet. Docker image validation can be added after a production hosting shape and container files are agreed.
