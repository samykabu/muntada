# Muntada Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-03

## Active Technologies
- C# / .NET 8+ (backend), TypeScript 5.x / React 18+ (frontend), Helm 3 / YAML (infrastructure) + ASP.NET Core 8, Entity Framework Core 8, MassTransit (RabbitMQ), FluentValidation, Serilog, OpenTelemetry SDK, .NET Aspire 13.2, xUnit, Moq (backend); React 18, Vite, Redux Toolkit + RTK Query, Axios, ESLint, Jest, Playwright (frontend) (000-foundation)
- SQL Server (per-module schemas), Redis (cache/session), RabbitMQ (messaging), MinIO (S3-compatible objects) (000-foundation)

- C# / .NET 8+ (backend), TypeScript 5.x / React 18+ (frontend), Helm 3 / YAML (infrastructure) + ASP.NET Core 8, Entity Framework Core 8, MassTransit (RabbitMQ), FluentValidation, Serilog, OpenTelemetry SDK, xUnit, Moq (backend); React 18, Vite, Redux Toolkit, Axios/React Query, ESLint, Jest, Playwright (frontend) (000-foundation)

## Project Structure

```text
aspire/    # .NET Aspire app host and local orchestration
backend/   # ASP.NET Core services, domain logic, and data access
frontend/  # React/Vite web application
infra/     # Helm charts, Kubernetes manifests, and environment config
```

## Commands

dotnet run --project aspire/Muntada.AppHost  # Primary local dev startup (Aspire 13.2)
npm test && npm run lint

## Code Style

C# / .NET 8+ (backend), TypeScript 5.x / React 18+ (frontend), Helm 3 / YAML (infrastructure): Follow standard conventions

## Recent Changes
- 000-foundation: Added C# / .NET 8+ (backend), TypeScript 5.x / React 18+ (frontend), Helm 3 / YAML (infrastructure) + ASP.NET Core 8, Entity Framework Core 8, MassTransit (RabbitMQ), FluentValidation, Serilog, OpenTelemetry SDK, .NET Aspire 13.2, xUnit, Moq (backend); React 18, Vite, Redux Toolkit + RTK Query, Axios, ESLint, Jest, Playwright (frontend)

- 000-foundation: Added C# / .NET 8+ (backend), TypeScript 5.x / React 18+ (frontend), Helm 3 / YAML (infrastructure) + ASP.NET Core 8, Entity Framework Core 8, MassTransit (RabbitMQ), FluentValidation, Serilog, OpenTelemetry SDK, xUnit, Moq (backend); React 18, Vite, Redux Toolkit, Axios/React Query, ESLint, Jest, Playwright (frontend)

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
