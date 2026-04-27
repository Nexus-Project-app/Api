# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build CleanArchitecture.slnx

# Run all tests
dotnet test CleanArchitecture.slnx

# Run a single test project
dotnet test tests/ArchitectureTests/ArchitectureTests.csproj

# Run a single test by name
dotnet test CleanArchitecture.slnx --filter "FullyQualifiedName~LayerTests.Domain_Should_NotHaveDependencyOnApplication"

# Start infrastructure (PostgreSQL + Seq)
docker-compose up -d

# Add a migration
dotnet ef migrations add <MigrationName> --project src/Infrastructure --startup-project src/Web.Api

# Apply migrations (also auto-applied on startup in Development)
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```

Seq (structured log viewer) is available at http://localhost:8081 when running via Docker.

## Architecture

This is a Clean Architecture solution targeting .NET 10. Dependency flow: `Domain ← Application ← Infrastructure` and `Domain ← Application ← Web.Api`. Architecture tests in `tests/ArchitectureTests` enforce these constraints with NetArchTest.

### Projects

- **SharedKernel** — Base types used by all layers: `Entity`, `Result<T>`, `Error`, `IDomainEvent`, `IDomainEventHandler`, `IDateTimeProvider`
- **Domain** — Entities and domain events. No dependencies on other layers.
- **Application** — Use cases (CQRS handlers), abstractions (interfaces only, no implementations). Depends only on Domain and SharedKernel.
- **Infrastructure** — EF Core/PostgreSQL, JWT authentication, permission authorization, domain event dispatching, Serilog/Seq. Implements Application abstractions.
- **Web.Api** — Minimal API endpoints, middleware, DI composition root.

### CQRS without MediatR

Commands and queries are dispatched by calling `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>` directly (injected as dependencies). Scrutor decorates all handlers automatically:

- `ValidationDecorator` — runs FluentValidation before the handler
- `LoggingDecorator` — logs execution time and errors

Registration happens via assembly scanning in `Application/DependencyInjection.cs`. Each use case folder contains the command/query, handler, and validator.

### Result pattern

All handlers return `Result` or `Result<T>`. Never throw for domain/application errors — use `Result.Failure(Error.xxx)`. Errors are defined as static fields on `*Errors` classes (e.g., `TodoItemErrors`, `UserErrors`). Endpoints call `result.Match(Results.Ok, CustomResults.Problem)` to convert to HTTP responses.

### Domain events

Entities inherit `Entity` (SharedKernel) and call `Raise(IDomainEvent)`. `DomainEventsDispatcher` in Infrastructure publishes events after `SaveChanges`. Event handlers implement `IDomainEventHandler<TEvent>` and are auto-registered via Scrutor.

### Endpoints

Each endpoint is a class implementing `IEndpoint` with a `MapEndpoint(IEndpointRouteBuilder)` method. Endpoints are discovered and registered via `AddEndpoints` / `MapEndpoints` in `Web.Api`. Tag constants live in `Endpoints/Tags.cs`.

### Authorization

Permission-based: decorate endpoints with `.RequireAuthorization(new HasPermissionAttribute("Permission.Name"))`. `PermissionProvider` resolves permissions from JWT claims.

## Code Style

From `.cursorrules` and `Directory.Build.props`:

- `TreatWarningsAsErrors` is enabled — all analyzer warnings fail the build
- Use explicit types; avoid `var` unless the type is immediately evident from the right-hand side
- Types are `internal sealed` by default unless a broader scope is required
- Use primary constructors for dependency injection
- Use `is null` / `is not null` instead of `== null` / `!= null`
- Prefer `record` for immutable DTOs and domain events
- Use `Guid` for entity identifiers
- Database column names use snake_case (via `EFCore.NamingConventions`)
- Central package version management in `Directory.Packages.props` — do not specify versions in individual `.csproj` files