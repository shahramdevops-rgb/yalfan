// Runs the screenshot comparison inside the pinned Playwright image (ADR 0028), the same on the
// developer's PC and in CI, because fonts render differently on Windows and Linux.
//   node scripts/screens.mjs            compare with the committed pictures
//   node scripts/screens.mjs --update   take the pictures again; review the diff before committing
// A failed comparison leaves the expected, actual and diff pictures in artifacts/check/screens.
import { spawnSync } from 'node:child_process';
import { mkdirSync, readFileSync, realpathSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// The tag says which Playwright; the digest makes sure the image is the one checked, so a tag
// moved by someone else cannot change what runs. Its version is the exact @playwright/test in
// package.json: the test runner and the image's browsers come from the same release.
const playwrightVersion = '1.65.0';
const image = `mcr.microsoft.com/playwright:v${playwrightVersion}-noble@sha256:9beb546099e11f52d62e505537914870e01a67e0e6b00ae41da4775324e73dbe`;

const update = process.argv.includes('--update');

// The real path, not the `subst` drive the check script runs from: Docker cannot share a subst drive.
const panel = realpathSync.native(resolve(dirname(fileURLToPath(import.meta.url)), '..'));

// An upgrade that moved the package but not the image would fail inside it on a missing browser,
// or take pictures with another Chromium: stop here and say what to change.
const lock = JSON.parse(readFileSync(resolve(panel, 'package-lock.json'), 'utf8'));
const locked = lock.packages?.['node_modules/@playwright/test']?.version;
if (locked !== playwrightVersion) {
  console.error(
    `@playwright/test is ${locked} in package-lock.json but the image is ${playwrightVersion}. ` +
      'Change the version and the digest in scripts/screens.mjs together with package.json (ADR 0028).',
  );
  process.exit(1);
}

const out = resolve(panel, '..', '..', 'artifacts', 'check', 'screens');
mkdirSync(out, { recursive: true });

// node_modules is a Docker volume with Linux builds of the packages, hiding the PC's own
// (Windows) node_modules; it is installed again only when package-lock.json changes.
const steps = [
  'set -e',
  'lock=$(sha256sum package-lock.json | cut -d " " -f 1)',
  'if [ "$(cat node_modules/.screens-lock 2>/dev/null)" != "$lock" ]; then',
  '  npm ci --no-audit --no-fund --loglevel=error',
  '  echo "$lock" > node_modules/.screens-lock',
  'fi',
  `npx playwright test${update ? ' --update-snapshots=changed' : ''}`,
].join('\n');

const docker = spawnSync(
  'docker',
  [
    'run',
    '--rm',
    '--init',
    // Chromium needs more shared memory than Docker's 64 MB. Playwright suggests --ipc=host, which
    // shares the host's instead; a larger memory of the container's own keeps it isolated.
    '--shm-size=1g',
    '-v',
    `${panel}:/work`,
    '-v',
    'yalfan-screens-node-modules:/work/node_modules',
    '-v',
    'yalfan-screens-npm-cache:/root/.npm',
    '-v',
    `${out}:/out`,
    '-w',
    '/work',
    '-e',
    'SCREENS_IN_IMAGE=1',
    '-e',
    'CI',
    image,
    'bash',
    '-c',
    steps,
  ],
  { stdio: 'inherit' },
);

if (docker.error) {
  console.error(`docker could not be started: ${docker.error.message}`);
}
process.exit(docker.status ?? 1);
