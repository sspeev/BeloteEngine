# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**BeloteEngine** is a backend server for a real-time multiplayer Belote card game, built on .NET 10.0 with ASP.NET Core and SignalR. The browser client lives in a separate repo.

## Solution Structure (Clean Architecture)

```
src/BeloteEngine.Domain/          -- Domain models & enums (no dependencies)
src/BeloteEngine.Application/     -- Application layer: contracts, services, rules, DTOs, CQRS commands
src/BeloteEngine.Infrastructure/  -- Infrastructure: EF Core DbContext, JWT provider, caching, in-memory store
src/BeloteEngine.Presentation/    -- API entry point: controllers, SignalR hub, DI extensions
tests/BeloteEngine.Unit.Tests/     -- xUnit unit tests
tests/BeloteEngine.Integration.Tests/ -- xUnit integration tests
tests/BeloteEngine.EndToEnd.Tests/    -- xUnit end-to-end tests
```

Dependency graph: Domain ← Application ← Infrastructure ↔ Presentation (Presentation depends on both Application and Infrastructure)

### Key files to know

- `BeloteEngine.slnx` — solution file (XML-based .slnx format, not legacy .sln)
- `src/BeloteEngine.Presentation/Program.cs` — application entry point & DI setup
- `src/BeloteEngine.Presentation/Hubs/BeloteHub.cs` — SignalR hub for real-time game communication
- `src/BeloteEngine.Application/Contracts/` — all service interfaces (ILobbyService, IGameService, IPlayValidator, etc.)
- `src/BeloteEngine.Application/Services/` — service implementations (LobbyService, GameService)
- `src/BeloteEngine.Application/Rules/` — game rule engines (PlayValidator, ScoreCalculator, TrickEvaluator)
- `src/BeloteEngine.Infrastructure/Data/BeloteEngineDbContext.cs` — EF Core DbContext
- `src/BeloteEngine.Presentation/Extensions/` — DI extension methods for WebApplicationBuilder & ServiceCollection

### Architecture patterns

- **Clean Architecture** with strict dependency inversion via interfaces in the Application layer
- **CQRS-style commands** for user registration/login (`RegisterUserCommand`, `LoginUserCommand`)
- **Validation strategy pattern** — separate validator interfaces (`ILobbyJoinValidation`, `ILobbyCreationValidation`)
- All services registered as singletons via DI

## Common Commands

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/BeloteEngine.Presentation

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/BeloteEngine.Unit.Tests
dotnet test tests/BeloteEngine.Integration.Tests
dotnet test tests/BeloteEngine.EndToEnd.Tests

# Run EF Core migrations
dotnet ef database update --project src/BeloteEngine.Infrastructure

# Create a new migration
dotnet ef migrations add <Name> --project src/BeloteEngine.Infrastructure
```

## Development Notes

- All projects target `<TargetFramework>net10.0</TargetFramework>` with nullable and implicit usings enabled.
- Test framework is xUnit 2.5.3 with coverlet for coverage.
- The `auth` branch (current) has recent work on user registration, JWT authentication, identity management, and connection registry.
- Configuration lives in `appsettings.json` — secret values use the `SecretKey` naming convention per recent commits.
- No `.cursorrules`, `.github/copilot-instructions.md`, or existing `CLAUDE.md` found.
