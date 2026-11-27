# ADR 0028 — Screenshots of the panel: Playwright in its pinned image, the API answered by sample data

- Status: accepted
- Date: 2026-11-26 (1405/09/05)
- Written in: roadmap task 1.7 (the first part of the design system)

## Context

Task 1.7 moves the panel onto a design system: about 280 raw colours in some forty files onto
tokens (1.7b), then new shared parts (1.7c, 1.7d). A visual mistake in that work fails no test:
Vitest checks logic and the build checks types, but nothing looks at the page. The developer
asked for automated screenshots of the main pages at desktop and phone width, compared on each
change, and on 1405/09/05 agreed to build them first, so the move onto tokens is checked by them.

Three things stand in the way of a picture that is the same on every run:
- fonts render differently on Windows and Linux, so a picture taken on the developer's PC never
  matches one taken in CI;
- the pages read the API, which needs a database and a login by SMS;
- the header shows today's date, and the pages show times and dates relative to it.

## Decision

1. **Playwright, inside its official Docker image, pinned by tag and digest**
   (`mcr.microsoft.com/playwright:v1.65.0-noble@sha256:…`), on the developer's PC and in CI alike,
   through `npm run screens` (`web/panel/scripts/screens.mjs`). Why: one image renders a page to
   the same pixels everywhere. The digest makes sure the image is the one that was checked, so a
   tag moved by someone else cannot change what runs. `@playwright/test` is pinned to the exact
   same version in `package.json` (installed the ADR 0014 way), not with a caret, and the script
   stops before Docker when `package-lock.json` holds another version, because the test runner
   and the image's browsers must come from one release (found by the review). `playwright.config.ts`
   refuses to run outside the image, so a picture is never taken on Windows by mistake, and writes
   a failed run's pictures only to the mounted `artifacts/check/screens`, never inside
   `web/panel`, where they could be committed.
   - The packages are installed inside the image into a Docker volume (Linux builds), again only
     when `package-lock.json` changes; the production build goes to `/tmp` in the container and
     is served by `vite preview`. So the PC's own `node_modules` and `dist` are never touched,
     and the `#` in the checkout's path (ADR 0014) does not matter inside the container. The
     script mounts the real path, not the `subst` drive the check script runs from, because
     Docker cannot share a `subst` drive.
   - Chromium only: the pictures are for seeing an unintended change of our own code, and a
     second browser would double them without finding more of our mistakes.
2. **The API is answered in the browser by fixed sample data** (`screens/api.ts`,
   `screens/sample.ts`), and the clock is frozen at 1405/09/04, ten in the morning in Tehran, with
   the browser in `fa-IR` and `Asia/Tehran`. Why: no database, SMS or backend is needed, every run
   sees the same page, and the whole run takes about fifteen seconds. The sample data:
   - is made up: common names, every mobile `0912 000 0xxx`, no real person or number;
   - is typed by the contract and type-checked by the panel build (`tsconfig.screens.json`), so
     an API change it no longer fits fails the build;
   - answers every route a main page calls; a request with no answer is answered 500 and fails
     the test naming it, and so does an error thrown in the page, instead of a picture of an
     error state.
3. **Fourteen main pages at two widths**: desktop 1280 × 800 and phone 390 × 844 (touch), each a
   picture of the whole page. The list is in `screens/pages.spec.ts`; a main page added to the
   panel is added there. The pictures are committed in `screens/__screenshots__`, named
   `<page>-<width>.png` (28 pictures, 1.6 MB). A picture is taken only once the page shows its
   data and no grey placeholder, network request or font load is left: each page that loads data
   names a text from the sample data it shows only after loading (a name, a fabric to tick, who
   made a save), and only the login page, which loads nothing, waits for its heading instead.
4. **No tolerance: one changed pixel fails** (`threshold: 0`, `maxDiffPixels: 0`), with the
   animations stopped and the caret hidden. Why: Playwright's default `threshold` of 0.2 passed a
   text turned from `stone-500` to `stone-600` when it was tried while building, the very change
   the move onto tokens can make by mistake; inside one pinned image the same page gives the same
   pixels, and repeated runs found no difference.
5. **It runs on every run of `scripts/check.ps1`** (step `panel screens`, about fifteen seconds
   once the image is pulled) **and in CI**, as the developer chose, because he runs the check
   before each commit. A failure prints each page and width with its reason («578 pixels are
   different», or the request that had no answer) and leaves the expected, actual and diff
   pictures in `artifacts/check/screens` (in CI, the artifact `screens`, kept seven days). An
   intended change is accepted with `npm run screens:update`, which takes again the pictures
   that differ and the missing ones of a page just added (all 28 were first taken this way), and
   the new pictures are reviewed in the diff and committed with the change.
6. **This changes ADR 0014 point 7:** Playwright arrives now, for the screenshots only. The
   end-to-end flows with the whole stack (database and SMS log) still wait for task 2.9, and will
   use the same image.

## Consequences

- A change that moves a pixel on a main page is committed with its new pictures, so the review
  sees what the operator will see.
- Each PC pulls about 1 GB once; CI pulls it on every run, which is why the panel job's timeout
  went from 10 to 15 minutes.
- Upgrading Playwright changes the tag and the digest in `scripts/screens.mjs` together with
  `package.json`, and usually means taking every picture again, because a new Chromium renders a
  little differently.
- A new API call on a main page needs its sample answer, or the test fails naming the request.
- `check.ps1` needs Docker for the panel too (it already did for the .NET tests).
- The container runs as root. On Windows (Docker Desktop) and in CI this changes nothing; on a
  Linux PC the files it writes in `web/panel` (the generated API types, updated pictures) would
  belong to root. Left as it is while the developer works on Windows.
- Not seen by the pictures: behaviour (clicks, the keyboard; tested since 1.7c by Playwright tests
  in the same image, ADR 0030),
  other browsers, and Chrome's Auto Dark Mode (ADR 0014).
