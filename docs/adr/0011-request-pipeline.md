# ADR 0011 — Request pipeline: handlers, validation, error responses, logging

- Status: accepted
- Date: 2026-11-18 (1405/08/27)
- Written in: roadmap task 0.4

## Context

Every feature from Phase 1 on is a use case behind an HTTP endpoint. Before the first one is
written, four things must be fixed once: how a use case is called, where its input is validated,
what an error looks like on the wire, and what is logged. The frontends (Persian panel, Persian
contractor app, fa/ar/en store) must turn any error into their own language, so errors carry codes,
never text (ARCHITECTURE.md 12). Numbers 0003 to 0010 are reserved for decisions planned in
ARCHITECTURE.md 14, so this ADR is 0011.

## Decision

### Use cases are plain handler interfaces, no mediator library

`ICommandHandler<TCommand, TResponse>` and `IQueryHandler<TQuery, TResponse>` in
`Application/Common/Handlers`, both returning `Task<Result<TResponse>>`. An endpoint injects the
handler interface and calls it.

| Choice | Why |
|---|---|
| No MediatR | It became commercial with a license key from version 13. A few interfaces are easier to read and debug than an indirect dispatch, for one developer |
| Commands and queries are separate interfaces | Later pipeline steps (transaction, audit) apply to commands only |
| A command always returns a value (usually the id of the row it created or changed) | One interface and one pipeline per kind, instead of a second "no value" set of interfaces and decorators |
| Handlers are `internal`; callers see only the interface | An endpoint cannot new up a handler and skip the pipeline |

### The pipeline is a decorator chain, registered by hand

`AddUseCases(assembly)` finds every handler in an assembly, registers it, and registers the
handler interface as the handler wrapped in the pipeline. Today the pipeline has one step,
validation; permission checks and transactions are added to the same chain later.

- **Validation is a decorator in Application, not an endpoint filter in Api** (the roadmap said
  "filter"). Why: ARCHITECTURE.md 5 puts validation and permissions inside the use case, so every
  caller (endpoint, Hangfire job, a later assistant) goes through them; an endpoint filter guards
  only HTTP.
- **Written by hand, not with Scrutor.** It is about 30 lines, adds no package, and wraps only
  the handlers of the assembly it is given, so calling it for a second assembly (the tests do)
  never wraps a handler twice. Two handlers for the same request throw at start.
- An invalid request never reaches its handler and comes back as a `Result` failure, not an
  exception.

### Validation errors carry fields and stable codes

- FluentValidation validators live next to their use case and are found by the same scan.
- A failure becomes `ValidationError` (Domain): code `validation.failed`, kind Validation, and one
  `FieldError(field, code, args)` per failing field. `Error` stopped being `sealed` for this one
  subtype.
- A rule may set its own code with `WithErrorCode("module.reason")`. Built-in rules report a C#
  class name (`NotEmptyValidator`), which must not become the contract with the frontends, so
  they are translated:

| Built-in rule | Code | Args |
|---|---|---|
| NotEmpty, NotNull | `validation.required` | |
| MaximumLength | `validation.too_long` | `maxLength` |
| MinimumLength | `validation.too_short` | `minLength` |
| Length | `validation.length` | `minLength`, `maxLength` |
| GreaterThan(OrEqual) | `validation.too_small` | `min`, `inclusive` |
| LessThan(OrEqual) | `validation.too_large` | `max`, `inclusive` |
| Inclusive/ExclusiveBetween | `validation.out_of_range` | `min`, `max`, `inclusive` |
| Email, Matches, CreditCard | `validation.invalid_format` | |
| anything else | `validation.invalid` | |

- Args are copied by name only; the value the user typed is never echoed, because it may be a
  password or an IBAN.
- Each rule stops at its first failure (`CascadeMode.Stop`), so a field reports one problem.

### Every error response is a coded ProblemDetails

RFC 9457 ProblemDetails with `code` and `traceId` on every error, from any source:

| Source | Status | Code |
|---|---|---|
| `ErrorType.Validation` | 400 | `validation.failed`, plus `errors: [{ field, code, args? }]` |
| `ErrorType.NotFound` | 404 | the error's code |
| `ErrorType.Conflict` | 409 | the error's code |
| `ErrorType.Forbidden` | 403 | the error's code |
| `ErrorType.Rule` | 422 | the error's code |
| `ErrorType.Unauthenticated` (added in 0.5, ADR 0012) | 401 | the error's code |
| No or invalid access token; wrong role | 401, 403 | `request.unauthenticated`, `request.forbidden` |
| Unreadable body, missing parameter | 400 | `request.malformed` |
| Unknown route, wrong method | 404, 405 | `request.not_found`, `request.method_not_allowed` |
| Any exception | 500 | `server.unexpected`, no message or stack trace |

- Rule is 422, not 400: the request was understood but the workshop's rules refuse it, and a
  frontend shows that differently from a typing mistake.
- Field paths use the JSON names the frontend sent (`lines[1].quantity`), so the message lands
  next to its field.
- A 500 shows nothing about the failure, because stack traces tell an outsider how the system is
  built. The full exception is logged under the same trace id.
- Endpoints answer with `result.ToOk()` / `error.ToProblem()`, typed results, so the OpenAPI
  document (task 0.8) describes the error shape.

### Endpoints are grouped in modules

Each module has an `IEndpointModule` that maps its routes; `Program.cs` maps every registered
module. Why: Program.cs stays a short list, and a test can register one more module and have it
served by the real pipeline.

### Logging is Serilog

- Configured from the `Serilog` section of appsettings: console always; Seq in Development
  (compose service `seq`, `http://localhost:5342`, no login because it is bound to this machine).
  Port 5342 because 5341 is often taken by another project's Seq. Sinks are named objects, not an
  array, so an environment file or variable can change one sink without replacing the others.
- One line per request: method, path, status, time, trace id. Bodies and query strings are never
  logged (passwords, phone numbers, IBANs). `/health` is logged at Verbose only, because the
  compose probe calls it every few seconds.
- A plain console logger exists before the host is built, so a crash during start (bad
  configuration, failed migration) still leaves a line.
- The production log format and where logs go are decided with deployment (3.1) and monitoring
  (14.3).

### Tests

`Yalfan.Api.IntegrationTests` is created in this task instead of 0.7, because the pipeline can be
tested without a database. The sample use case and its endpoint exist only in the test project,
so the product carries no fake feature. Task 0.7 adds a real PostgreSQL to the same project.

## Consequences

- A new use case is: command or query record, validator, `internal` handler, and one line in its
  module's `IEndpointModule`. Nothing else is registered by hand.
- A new error code is a contract: once released it is never renamed, and every frontend maps it.
- Adding a pipeline step (permissions, transaction) means one decorator and one line in
  `UseCaseRegistration`.
- `ValidatorOptions.Global` is set once in `AddApplication()`; it is process-wide, which is fine
  for one API process.
- Added in 1.6.4 (ADR 0025): every answer under `/panel`, errors included, carries
  `Cache-Control: no-store`, set by a small middleware before the error handler.
