.PHONY: setup aspire up down clean test docker-build logs help

## Primary local dev — uses Aspire (Constitution XII)
setup: ## Full setup: restore dependencies + start Aspire
	dotnet restore aspire/Muntada.AppHost/Muntada.AppHost.csproj
	cd frontend && npm ci
	dotnet run --project aspire/Muntada.AppHost

aspire: ## Start Aspire AppHost (primary method)
	dotnet run --project aspire/Muntada.AppHost

## Fallback — Docker Compose (NOT primary per Constitution XII)
up: ## Start Docker Compose services (fallback)
	docker-compose up -d

down: ## Stop all services
	docker-compose down

clean: ## Remove all containers, volumes, and build artifacts
	docker-compose down -v --remove-orphans
	rm -rf backend/src/*/bin backend/src/*/obj
	rm -rf backend/tests/*/bin backend/tests/*/obj
	rm -rf frontend/node_modules frontend/dist

## Testing
test: test-backend test-frontend ## Run all tests

test-backend: ## Run backend unit tests (xUnit)
	dotnet test backend/tests/Muntada.SharedKernel.Tests/

test-frontend: ## Run frontend unit tests
	cd frontend && npm run test:unit

test-e2e: ## Run Playwright E2E tests
	cd frontend && npx playwright test

## Build
docker-build: ## Build Docker images
	docker build -f backend/src/Muntada.Api/Dockerfile -t muntada-api:latest .
	docker build -f frontend/Dockerfile -t muntada-frontend:latest frontend/

## Logs
logs: ## Tail service logs (Docker Compose)
	docker-compose logs -f

## Help
help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'
