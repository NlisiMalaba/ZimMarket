# Contributing to ZimMarket

Thanks for contributing. This guide covers local setup, running tests, adding new features with the CQRS flow used in this repository, Docker commands, and branch/PR workflow.

## Local Setup

### Prerequisites

- .NET SDK 10
- Docker Desktop
- Git

### Clone and build

```bash
git clone <your-remote-url> ZimMarket
cd ZimMarket
dotnet restore ZimMarket.sln
dotnet build ZimMarket.sln
```

### Environment configuration

Create a local `.env` file in the repository root and set required values for local development (database, redis, jwt, storage, integrations).

- Do not commit `.env`.
- Keep real secrets out of source control.

## Running the Application

### API from source

```bash
dotnet run --project src/ZimMarket.API/ZimMarket.API.csproj
```

OpenAPI (development only):

- `http://localhost:<port>/openapi/v1.json`
- `http://localhost:<port>/scalar`

### Full stack with Docker Compose

```bash
docker compose up --build
```

Useful variants:

```bash
docker compose up -d
docker compose logs -f
docker compose down
docker compose down -v
```

Health check:

- `GET http://localhost:8080/health` (or your configured API port)

## Running Tests

Run all tests:

```bash
dotnet test ZimMarket.sln
```

Run per test project:

```bash
dotnet test tests/ZimMarket.Domain.Tests/ZimMarket.Domain.Tests.csproj
dotnet test tests/ZimMarket.Application.Tests/ZimMarket.Application.Tests.csproj
dotnet test tests/ZimMarket.Integration.Tests/ZimMarket.Integration.Tests.csproj
```

Integration tests use Testcontainers and require Docker to be running.

## Load Test (k6)

```bash
k6 run tests/ZimMarket.Integration.Tests/product-search-load.k6.js
```

Optional environment variables:

- `BASE_URL` (default `http://localhost:5000`)
- `SEARCH_TERM` (default `integration`)
- `PAGE_SIZE` (default `20`)

## Adding a New Feature (CQRS Flow Template)

Follow Clean Architecture boundaries:

- Domain: entities, value objects, domain events, interfaces
- Application: command/query, handler, validator, DTOs
- Infrastructure: repository/service implementations
- API: controller endpoint and request contracts

### 1) Domain

- Add or update entities/value objects in `src/ZimMarket.Domain`.
- Add domain events when state changes matter outside the aggregate.
- Add or update repository interfaces in `src/ZimMarket.Domain/Interfaces`.

### 2) Application

- Add a command or query and response DTO in `src/ZimMarket.Application/<Module>`.
- Implement handler using interfaces from `Domain`/`Application.Common.Interfaces`.
- Add FluentValidation validator (`AbstractValidator<TRequest>`).
- Ensure handler returns `Result`/`Result<T>` and uses domain error codes consistently.

### 3) Infrastructure

- Implement data access or service integration in `src/ZimMarket.Infrastructure`.
- Register implementation in DI (`src/ZimMarket.Infrastructure/DependencyInjection.cs`).
- Add or update EF Core mappings/migrations if persistence schema changes.

### 4) API

- Add endpoint in the appropriate controller under `src/ZimMarket.API/Controllers/V1`.
- Keep controllers thin: map request -> command/query -> `ISender`.
- Add authorization policy attributes where required.
- Ensure OpenAPI examples/security/error responses remain accurate.

### 5) Tests

- Add/extend:
  - Domain tests for business rules
  - Application tests for handler behavior
  - Integration tests for endpoint flow
- Run `dotnet test ZimMarket.sln` before opening a PR.

### Minimal command/query skeleton

```csharp
public sealed record CreateThingCommand(string Name) : IRequest<Result<Guid>>;

public sealed class CreateThingCommandValidator : AbstractValidator<CreateThingCommand>
{
    public CreateThingCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class CreateThingCommandHandler : IRequestHandler<CreateThingCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateThingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateThingCommand request, CancellationToken cancellationToken)
    {
        // 1) Create domain object
        // 2) Persist through repository/UoW
        // 3) Return Result.Success(id) or Result.Failure(...)
        throw new NotImplementedException();
    }
}
```

## Branching Strategy

- Base branch: `main`
- Branch naming:
  - `feat/<short-description>`
  - `fix/<short-description>`
  - `chore/<short-description>`
  - `docs/<short-description>`
  - `test/<short-description>`

Examples:

- `feat/module-16-openapi-hardening`
- `fix/auth-refresh-edge-case`

## Pull Request Guidelines

- Keep PRs focused and reasonably small.
- Link the relevant task(s) from `planning/TASKS.md`.
- Include:
  - What changed
  - Why it changed
  - How it was tested
  - Any config or migration impact
- Ensure CI passes and OpenAPI docs remain accurate.

## Coding Standards

- Follow SOLID and clean architecture boundaries.
- Keep business logic out of controllers.
- Prefer dependency injection and interface-driven integrations.
- Validate inputs with FluentValidation.
- Add logging and error handling for external IO.
- Avoid raw SQL with user input.
