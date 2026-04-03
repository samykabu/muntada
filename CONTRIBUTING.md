# Contributing to Muntada

## Getting Started

1. Clone the repo and follow the [README](README.md) Quick Start
2. Create a feature branch: `git checkout -b <number>-<feature-name>`
3. Implement following the task breakdown in `specs/<feature>/tasks.md`

## Development Rules (Constitution)

- **Aspire first**: Local dev via `dotnet run --project aspire/Muntada.AppHost`
- **Commit per task**: One Git commit per completed task
- **Tests before commit**: All unit tests must pass (`make test`)
- **XML docs**: All public C# types/methods require `/// <summary>` documentation
- **DRY / KISS**: No code duplication, keep it simple
- **Reusable components**: React elements used in 2+ places go to `frontend/src/shared/components/`

## Database Migrations

**NEVER generate migrations with AI tools.** Always use:

```bash
dotnet ef migrations add <Name> --project backend/src/Modules/<Module>/Infrastructure
```

## Pull Request Process

1. Create GitHub issues for each task before starting
2. Implement and commit per task
3. Run code review (`/code-review`)
4. Fix all findings
5. Create PR with detailed summary
6. Ensure CI passes (lint, test, build)

## Module Registration

Every new backend module MUST be registered in `aspire/Muntada.AppHost/AppHost.cs`.

## Branch Naming

- `<number>-<feature-name>` (e.g., `001-identity-access`)
- Follows the epic order in `specs/INDEX.md`
