# ADR 0001 — Modular monolith with Clean Architecture

- Status: accepted
- Date: 2026-11-15 (1405/08/24)
- Written in: roadmap task 0.1

## Context

Yalfan is built and run by one developer for one workshop: about 60 staff and contractor users,
plus a small store. Money, stock and production must stay consistent with each other: one
contractor delivery changes stock, a work order and a ledger at the same moment. The developer
already runs a system of the same shape (Gym Management), so its patterns and lessons carry over.

## Decision

**One deployable backend, one PostgreSQL database, four projects.**

```
Api  →  Application  →  Domain
          ↑
    Infrastructure (implements Application interfaces)
```

| Project | References | Why |
|---|---|---|
| `Yalfan.Domain` | nothing, and no packages | Business rules must be testable without a database or a web server, and must survive a change of framework |
| `Yalfan.Application` | Domain | Use cases depend on interfaces, never on a provider (EF Core provider, SMS, bank) |
| `Yalfan.Infrastructure` | Application | It implements those interfaces; swapping a provider touches only this project |
| `Yalfan.Api` | Application, Infrastructure | It is the composition root. It references Infrastructure only to register it; endpoints call Application use cases |

- **Modules are folders, not projects.** Each module (Catalog, Production, Ledger, …) has a
  folder of the same name in Domain, Application and Api. Why: features change together, so the
  code of one feature should sit together; and one project per module per layer would be dozens
  of projects for one developer.
- **Each layer exposes one DI extension**: `AddApplication()` and
  `AddInfrastructure(configuration)`. Why: `Program.cs` stays a short list and a layer's wiring
  lives inside the layer.
- **The dependency rule is enforced, not only written down.** Project references make a wrong
  direction a compile error, and `DomainDependencyTests` fails when Domain code uses anything
  outside the .NET base class library.

### Skeleton choices made with this ADR

| Choice | Why |
|---|---|
| .NET 10 (`net10.0`), not .NET 11 | 10 is the long-term-support release (patched for three years); 11 came out this month and is supported for a shorter time. A workshop system should not need a forced upgrade every year. Same version as the gym project |
| `global.json` pins the SDK (`10.0.401`, `rollForward: latestFeature`) | The developer's machine and CI build with the same SDK line, so "works on my machine" differences do not appear |
| `Directory.Build.props` holds target framework, nullable, implicit usings, warnings as errors | One place to change; project files hold only what is special to them. Warnings as errors from day one, because turning it on later means fixing hundreds at once |
| NuGet audit warnings (`NU190x`) are not errors | They depend on the outside world (a new advisory, nuget.org unreachable) and must not break a build that passed yesterday. Dependency audit is a task of its own (14.1) |
| Central package versions (`Directory.Packages.props`) | One version of each package for the whole solution; an upgrade is a one-line change |
| Microsoft packages stay on `10.0.x` | They must match the `net10.0` runtime. `dotnet add package` offers 11.x by default and that must be declined |
| `Yalfan.slnx` | The current solution format: short, readable XML that merges cleanly. Same as the gym project |
| xUnit v3 on Microsoft.Testing.Platform | xUnit v3 is the maintained line, and on the .NET 10 SDK it runs only on the new platform, so `global.json` selects that runner |
| Only `Yalfan.Domain.Tests` for now | `Yalfan.Api.IntegrationTests` needs a database and auth to mean anything; it is created in task 0.7 |

## Alternatives considered

- **Microservices.** Rejected: one developer and about 60 users do not need them, and they would
  put money and stock in different databases, turning one transaction into distributed
  consistency work.
- **One project with folders for layers.** Rejected: nothing would stop Domain code from using
  EF Core or HTTP types; the compiler would no longer guard the rule.
- **One project per module** (vertical slices as assemblies). Rejected for now: stronger module
  walls, but many more projects and slower builds. Revisit only if modules start leaking into
  each other.

## Consequences

- A use case that touches several modules runs in one database transaction.
- Module boundaries are a convention plus review, not a compiler rule: a module changes another
  module's data only through that module's handlers or domain methods (ARCHITECTURE.md, 5).
- Scaling means a bigger server, not more services. That is enough for this workload.
- Moving to a new .NET version is one deliberate change: `global.json`, `Directory.Build.props`
  and `Directory.Packages.props` together.
