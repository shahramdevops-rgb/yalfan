# ADR 0014 — Panel shell: same origin, token in memory, a committed OpenAPI contract

- Status: accepted
- Date: 2026-11-19 (1405/08/28)
- Written in: roadmap task 0.8

## Context

The staff panel (`web/panel`) is the first frontend. It has to call the API, keep the SMS login
of ADR 0012 safe in a browser, get types for every API call, show Persian, Jalali dates and Rial,
and stay light on the workshop's mobile internet. The contractor app (Phase 10) will copy what is
decided here.

## Decision

1. **The panel and the API share one origin; there is no CORS.** In development Vite forwards
   `/panel/*` to the API; in production the reverse proxy does the same (task 3.1). Why:
   - the refresh cookie (SameSite=Strict, Path `/panel/auth`, ADR 0012) works as it is;
   - a cross-origin call with an `Authorization` header needs a preflight request every time, an
     extra round trip on mobile internet;
   - CORS with credentials is a common source of security mistakes, and nothing needs it.

   This replaces the line in ADR 0012 that planned CORS for the panel origin in 0.8.
   Consequence: no page of the panel may have a path under `/panel/`, because those requests go
   to the API.

2. **The access token lives in memory only**, inside the session store's closure, never in
   localStorage (ADR 0012). After a reload it is gone and the panel renews the session with the
   HttpOnly refresh cookie before showing anything. Renewal also happens when a call answers
   401. Rules, each with a unit test:
   - one renewal at a time: calls that fail together share it, so a refresh token that was just
     rotated is never sent twice;
   - `identity.refresh_superseded` (another tab renewed first) is tried once more after one
     second, when the other tab's response has brought the new cookie;
   - a 401 from the login routes is an answer (a wrong code), never a reason to renew;
   - when the API cannot be reached, an existing session is kept; only the first check at
     start-up ends signed out.

3. **One OpenAPI document per frontend, committed as the contract.** The API builds a `panel`
   document with only the `/panel` routes, served in Development and Testing only (the live API
   has no reason to publish its map, and the panel's types should not show store endpoints). It
   is committed as `web/panel/openapi/panel.json`. `PanelDocumentTests` compares it with what the
   API serves; when they differ the test rewrites the file and fails, so `git diff` shows the
   contract change and CI is red until it is committed. The TypeScript types
   (`src/shared/api/schema.d.ts`) are generated from that file before every dev, build and test
   run and are not committed. Why: one reviewable file, drift caught by CI, and types that can
   never be older than the file. The generator runs through a small script
   (`scripts/generate-api-types.mjs`) because the openapi-typescript CLI breaks on a `#` in the
   path.

4. **Numbers travel only as JSON numbers** (`JsonNumberHandling.Strict`). The web default also
   accepts `"5"` for 5, which made the contract say "integer or string" and the panel's types
   `number | string`. The API now answers `request.malformed` to a number sent as text.

5. **Stack:** React 19, Vite, Tailwind 4, React Router, TanStack Query, `openapi-fetch` and npm,
   the same as the gym panel so habits carry over. TypeScript stays on 5.9 because
   openapi-typescript supports only TypeScript 5. Packages are installed with
   `npm install --before=<two weeks ago>`, so a version published in the last days, the usual
   window of a hijacked release, is never picked; `npm ci` then installs exactly the lock file.

6. **Persian display without libraries:**
   - Vazirmatn is bundled with the panel, not loaded from a font CDN that may be slow or blocked
     in Iran; Tahoma is the fallback.
   - Jalali dates and Persian digits come from the browser's `Intl` (`fa-IR`, Persian calendar).
     A moment is shown in `Asia/Tehran`, never in the browser's zone; a calendar day from the
     API is shown in UTC so it never shifts. The weekday is formatted on its own and put in front
     by hand, because ICU's combined pattern differs between versions. The frontend tests run
     in `America/Los_Angeles` to catch any reliance on the machine's zone.
   - Every text is in `src/shared/i18n/fa.ts` with a typed `t()`, and API error codes map to
     Persian there, with a fallback for unknown codes. No i18n library: the panel has one
     language.
   - Amounts are formatted by `formatRial`: Persian digits, separators and the «ریال» label
     (ADR 0002).

7. **Not now:** `web/shared` waits for the second app (Phase 10), because a workspace for one
   app is only overhead; Playwright waits for the first real workflow (2.9), because it needs
   the whole stack, database and SMS log included, in CI. *Changed 1405/09/05 (ADR 0028):*
   Playwright arrived in task 1.7 for screenshots of the main pages only, with the API answered
   by sample data in the browser; the flows with the whole stack still wait for 2.9.

## Consequences

- The production reverse proxy (3.1) must send `/panel/*` to the API and everything else to the
  panel's static files, with `index.html` as the fallback for client-side routes.
- An API change under `/panel` is committed together with the rewritten contract file.
- Vite and Vitest cannot run from a path that contains `#`. Decided by the developer on
  1405/08/28: the checkout moves out of `c#project`; until then the panel runs from a `subst`
  drive letter. CI runs on Linux and is not affected. *Done 1405/09/06:* the checkout moved to
  `D:\Yalfan` and `npm run dev` runs there as it is (checked: Vite served the page and
  `src/main.tsx`). It was copied rather than moved, because the open editor holds the old
  folder; the old copy is deleted once the editor is reopened on the new one. `check.ps1` keeps
  its `subst` fallback for any checkout whose path contains `#`.
- Chrome's Auto Dark Mode darkens the light panel for users who turned it on. Left alone on
  purpose (decided 1405/08/28): it is the user's choice and stays readable; a real dark theme
  can come later.
