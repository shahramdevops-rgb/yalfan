# Yalfan

Management panel and online store for the Yalfan bag workshop (Hamedan, Iran): about 20 in-house
workers and about 40 outside contractors.

**Status:** Phase 0 (foundation) in progress. The solution skeleton, the database wiring, the
common building blocks, the request pipeline, staff login, the audit and the integration test
base with CI, and the Persian panel's shell with login exist (tasks 0.1 to 0.8); there are no
business features yet.

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — layers, modules, core models, attendance, languages
- [docs/ROADMAP.md](docs/ROADMAP.md) — phases 0–15, tasks, the owner's own to-do list, open questions

In one line: a .NET modular monolith (Clean Architecture, PostgreSQL) behind three web apps: a
Persian staff panel, a phone panel for contractors, and a three-language store.

Build and test (needs the .NET 10 SDK, and Docker running: the integration tests start their
own PostgreSQL with Testcontainers):

```
dotnet build Yalfan.slnx
dotnet test --solution Yalfan.slnx
```

CI (GitHub Actions, [.github/workflows/ci.yml](.github/workflows/ci.yml)) runs the same build and
tests on every push.

Run the whole stack in Docker (PostgreSQL, the API and Seq):

```
copy .env.example .env          # once; then set POSTGRES_PASSWORD, JWT_SIGNING_KEY and the first owner in .env
docker compose up --build --wait
curl http://localhost:8080/health
```

Logs: the console, and in Development also Seq at http://localhost:5342 (no login; bound to this
machine only).

Log in (there are no passwords; ADR 0012). In Development no SMS is sent: the code is written to
the API log and Seq as `SMS not sent (development log provider) … {"code": "123456"}`.

```
POST /panel/auth/otp     { "mobileNumber": "09121234567" }
POST /panel/auth/login   { "mobileNumber": "09121234567", "code": "123456" }
```

Run the API from the host against the database in Docker (the user-secrets lines once):

```
docker compose up db seq --wait
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5433;Database=yalfan;Username=yalfan;Password=<the password in .env>" --project src/Yalfan.Api
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 random characters>" --project src/Yalfan.Api
dotnet user-secrets set "Identity:SeedOwner:FullName" "<owner's name>" --project src/Yalfan.Api
dotnet user-secrets set "Identity:SeedOwner:MobileNumber" "<owner's mobile>" --project src/Yalfan.Api
dotnet run --project src/Yalfan.Api          # http://localhost:5080/health
```

Run the panel (needs Node 24, see `web/panel/.nvmrc`; the API runs from the host as above). Vite
cannot run from a path that contains `#`, so keep the checkout out of folders like `c#project`
(ADR 0014):

```
cd web/panel
npm ci                          # once, and after package-lock.json changes
npm run dev                     # http://localhost:5173, /panel/* is forwarded to the API on :5080
npm test
npm run build
```

Set `YALFAN_API_URL` to reach an API elsewhere, e.g. `http://localhost:8080` for the API in
Docker. The panel's API types come from `web/panel/openapi/panel.json`; when an API change
under `/panel` makes `PanelDocumentTests` fail, the test has rewritten that file: review the
diff, commit it, and the panel picks the new types up on its next run.

Add a migration after changing the model:

```
dotnet tool restore             # once
dotnet ef migrations add <Name> --project src/Yalfan.Infrastructure --startup-project src/Yalfan.Api --output-dir Persistence/Migrations
```

Next step: task 0.9 (CI lint and format checks). The Phase 1 questions must be answered before Phase 1.
