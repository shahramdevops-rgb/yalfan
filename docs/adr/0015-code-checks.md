# ADR 0015 — Code checks: format and lint on every build and every push

- Status: accepted
- Date: 2026-11-20 (1405/08/29)
- Written in: roadmap task 0.9

## Context

CI has built the solution and run every test on each push since task 0.7, and the panel since
0.8. Nothing checked layout or the rules that the compiler does not see. The developer works
alone, so no reviewer catches them either. Several rules in CLAUDE.md were only written down:
logical CSS utilities for right-to-left, no user-facing text inside components. The project
already turns written rules into build errors where it can (`BannedSymbols.txt`, ADR 0005 and
0013); this ADR does the same for format and lint.

## Decision

1. **LF line endings everywhere** (`.gitattributes`: `* text=auto eol=lf`; `.editorconfig`:
   `end_of_line = lf`). Why: with `core.autocrlf` on Windows, part of the checkout was CRLF, and
   `dotnet format` on Windows asked for CRLF where Linux CI would ask for LF. A format check that
   answers differently on two machines is worse than none.
2. **.NET lint runs inside the build.** `AnalysisMode` is `Recommended` and
   `EnforceCodeStyleInBuild` is on (`Directory.Build.props`); a few code style rules are raised to
   warning in `.editorconfig` (file-scoped namespace, braces, readonly field, unused private
   member or parameter). Warnings are already errors, so a violation fails the build in the editor
   and in CI alike, with nothing extra to run. When adopted, the recommended rules found six
   problems; the one that mattered was CA1305 (formatting without a culture), which on a machine
   with Persian culture can write Persian digits into logs or data.
   Exceptions, each with its reason in `.editorconfig`:
   - CA1707 (underscores in names) is off in `tests/`: test names read as
     `Method_Situation_Result` on purpose (276 hits).
   - CA1716 (a name that is a keyword in another .NET language) is off: it objects to `Error`
     because it is a Visual Basic keyword, and the solution is C# only.
   - Migrations are `generated_code`: `dotnet ef` writes them, with a BOM and its own style, and
     rewrites them on every model change; neither the formatter nor the analyzers judge them.
   - **Not adopted: the unused-using rule (IDE0005) in the build.** It needs
     `GenerateDocumentationFile`, and with that on, the OpenAPI generator copies the XML comments
     into the panel's contract (`panel.json`). Those comments are notes for developers, and they
     carried `\r\n` on Windows. The contract matters more than a style rule; editors still grey
     out unused usings.
3. **.NET format is a separate CI step**, `dotnet format whitespace --verify-no-changes`, not part
   of the build. Why: a misplaced space should not stop a debugging build. `dotnet format
   whitespace Yalfan.slnx` fixes everything it reports.
4. **Panel lint is ESLint** with typescript-eslint's type-checked recommended rules (they find a
   promise nobody awaits, which the compiler does not), react-hooks and react-refresh. Warnings fail
   it too (`--max-warnings 0`), as in .NET. Two project rules turn CLAUDE.md into errors:
   - Physical-direction Tailwind utilities (`ml-`, `pr-`, `left-`, `text-right`, `border-l`,
     `rounded-tr`...) are refused in `src/`; only the logical ones are allowed, because the panel
     is right-to-left and the store will also be left-to-right.
   - Persian text inside a component (`.tsx`) is refused: every word the user reads comes from
     `src/shared/i18n`. The formatting helpers (`src/shared/format`, `.ts`) may still produce
     Persian such as «ریال».
   Both are `no-restricted-syntax` patterns in `eslint.config.js`; no extra plugin was needed.
5. **Panel format is Prettier** with single quotes, 120 columns and the Tailwind plugin, which
   keeps class lists in one order. Why 120: it is how the panel was already written (at 120,
   Prettier rewrapped 7 of about 25 files; at 100 it would have been 12). Indentation comes from
   `.editorconfig` (2 for TypeScript, CSS and HTML). `openapi/panel.json` is excluded because
   `PanelDocumentTests` writes it in System.Text.Json's layout; `package-lock.json` because npm
   owns it.
6. **CI runs every check even when an earlier one failed** (`if: !cancelled()`), and skips a
   check only when what it needs is missing (no restore, no install, or no build for the .NET
   tests). Why: one push reports every problem instead of one problem per push.
7. **Fixed during adoption** (code, not rules): the dashboard threw a plain object from a query, so
   `error` was typed as `Error` but was not one. Queries now throw `ProblemError`, which
   `toProblem` reads back; every module will copy this from Phase 1. The drawer closed through a
   `setState` inside an effect (an extra render); it now stays open only on the page where it was
   opened. Session store members are arrow functions, because React calls them detached.

## Consequences

- Before a commit: `dotnet format whitespace Yalfan.slnx`, and in `web/panel` `npm run format`
  and `npm run lint` (CLAUDE.md, Commands).
- A new code style rule or analyzer exception goes into `.editorconfig` with its reason.
- The API's Dockerfile copies `.editorconfig`: without it the exceptions are missing and the image
  build fails on CA1716 (checked when this was written).
- `npm audit` reports a moderate advisory in `minimatch` (GHSA-g3hv-75wg-m249) through ESLint and
  typescript-eslint. Accepted: they are development tools that only expand the project's own
  glob patterns, and the fix is minimatch 11, which ESLint 10 does not accept yet. Revisit when
  ESLint moves to it.
- Proving that CI turns red needs the developer's push: a throwaway branch `ci/prove-red` carries a
  failing xUnit test, a failing Vitest test, a C# whitespace error, an ESLint error and a Prettier
  error, and is deleted after the run.
