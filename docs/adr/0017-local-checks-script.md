# ADR 0017 — Local checks: one script with short output

- Status: accepted
- Date: 2026-11-22 (1405/09/01)
- Written in: outside the roadmap, at the developer's request

## Context

The checks of ADR 0015 are seven commands in two folders. Vite and Vitest also need a `subst`
drive letter while the checkout sits in `c#project` (ADR 0014).

More important, most of the work is done with an AI assistant, which reads every line a command
prints. Those lines stay in the conversation for the rest of the session and are read again on
every later turn. Measured on 1405/09/01: a successful build prints 14 lines; a test run with
Docker stopped printed 4,277 lines (528 KB, about 130,000 tokens), the same Testcontainers error
104 times with its stack trace.

## Decision

1. **`scripts/check.ps1` runs the steps of `ci.yml`**: dotnet format, build and test; panel lint,
   format, build and test. It prints one line per step; the full output of each step goes to
   `artifacts/check/<step>.log` (ignored by git). As in CI, every step runs even when an earlier
   one failed, and a step is skipped only when what it needs is missing.
2. **A failed step prints only what is needed to fix it:**
   - compiler, analyzer and format messages once each (MSBuild prints each one twice), with paths
     relative to the repository;
   - failed .NET tests grouped by their error: the message (at most 8 lines), the first stack
     frame in Yalfan code, and up to three test names per group. The run above becomes two
     groups, about 20 lines;
   - panel output without npm's banners, blank lines and passed Vitest files, cut to its first 30
     and last 10 lines when it is longer.

   A passed test step prints its counts, which is the evidence every task ends with (CLAUDE.md).
3. **Docker is checked before the .NET tests**, so a stopped Docker is one line, not 104 failures.
4. **The panel runs from a `subst` drive letter by itself** when the path contains `#`: an existing
   mapping when one covers the checkout, otherwise a free letter that is removed at the end.
5. **`npm ci` runs only when `package-lock.json` is newer than the last install**, because it
   deletes and rewrites `node_modules` every time.
6. **PowerShell, compatible with Windows PowerShell 5.1**, because the developer's machine is
   Windows and needs nothing installed for it. The file stays ASCII: Windows PowerShell reads a
   file without a byte order mark in the ANSI code page.
7. **CI keeps the plain commands.** A person reads its logs in the browser and wants them whole.

## Alternatives

- **A separate AI agent that runs the checks and reports back.** Not taken. It keeps the output
  out of the main conversation, but it still reads all of it itself: the tokens move instead of
  shrinking, and a summary written by a model can drop the one line the fix needs. It can be put
  on top of this script later if the output is still too long.
- **Quieter flags only** (`--verbosity quiet` and the like). Not enough: they neither group a
  hundred identical test failures nor drop MSBuild's repeated errors.

## Consequences

- A step added to or changed in `ci.yml` is changed in the script too.
- The grouping depends on the output format of Microsoft.Testing.Platform and MSBuild. When that
  changes and nothing is recognised, the script prints the head and tail of the output instead, so
  a failure is never hidden. Pass or fail always comes from the exit code, never from the parsing.
