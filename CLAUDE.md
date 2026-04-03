# Muntada Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-03

## Active Technologies
- C# / .NET 10 (backend), TypeScript 5.x / React 19 (frontend), Helm 3 / YAML (infrastructure) + ASP.NET Core 10, Entity Framework Core 10, MassTransit (RabbitMQ), FluentValidation, Serilog, OpenTelemetry SDK, .NET Aspire 13.2, xUnit, Moq (backend); React 19, Vite, Redux Toolkit + RTK Query, Axios, ESLint, Jest, Playwright (frontend) (000-foundation)
- SQL Server (per-module schemas), Redis (cache/session), RabbitMQ (messaging), MinIO (S3-compatible objects) (000-foundation)

## Project Structure

```text
aspire/            # .NET Aspire AppHost and ServiceDefaults
backend/           # ASP.NET Core 10 services, SharedKernel, tests
frontend/          # React 19 / Vite / TypeScript SPA
infra/             # Helm charts, Kubernetes manifests
docs/              # Architecture diagrams, runbooks
specs/             # Feature specifications and task breakdowns
```

## Commands

dotnet run --project aspire/Muntada.AppHost  # Primary local dev startup (Aspire 13.2)
npm test && npm run lint

## Code Style

C# / .NET 10 (backend), TypeScript 5.x / React 19 (frontend), Helm 3 / YAML (infrastructure): Follow standard conventions

## Recent Changes
- 000-foundation: .NET 10, Aspire 13.2.1, MassTransit 9.1.0, MediatR 14.1.0, FluentValidation 12.1.1

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
