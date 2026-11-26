import { defineConfig } from '@playwright/test';

// Screenshots of the main pages at desktop and phone width, compared with the pictures committed
// in screens/__screenshots__ (ADR 0028). They run only inside the pinned Playwright image, through
// `npm run screens`: fonts render differently on Windows and Linux, so a picture taken outside the
// image would never match one taken inside it.
if (!process.env.SCREENS_IN_IMAGE) {
  throw new Error(
    'Run the screenshots with `npm run screens` (or `npm run screens:update`), not with playwright directly.',
  );
}

const port = 4173;

export default defineConfig({
  testDir: 'screens',
  // One picture per page and width, with no operating system in the name: there is only one.
  snapshotPathTemplate: '{testDir}/__screenshots__/{arg}-{projectName}{ext}',
  // artifacts/check/screens, mounted by scripts/screens.mjs; never a folder inside web/panel, where
  // the pictures of a failed run could be committed by mistake.
  outputDir: '/out',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  // A picture that differs is a finding, not bad luck: no retries.
  retries: 0,
  reporter: [['list']],
  expect: {
    // Any changed pixel fails. Playwright's default `threshold` of 0.2 lets a pixel's colour move
    // a fifth of the way unseen, and passed a text turned from stone-500 to stone-600, the very
    // change moving the screens onto tokens (1.7b) can make by mistake. Inside one pinned image
    // the same page renders to the same pixels, so no tolerance is needed. The animations are
    // stopped and the caret hidden, which would otherwise differ from run to run.
    toHaveScreenshot: { threshold: 0, maxDiffPixels: 0, animations: 'disabled', caret: 'hide', scale: 'css' },
  },
  use: {
    baseURL: `http://127.0.0.1:${port}`,
    locale: 'fa-IR',
    timezoneId: 'Asia/Tehran',
    colorScheme: 'light',
    serviceWorkers: 'block',
  },
  projects: [
    { name: 'desktop', use: { browserName: 'chromium', viewport: { width: 1280, height: 800 } } },
    {
      name: 'phone',
      use: { browserName: 'chromium', viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true },
    },
  ],
  // The production build, as the workshop will get it, served by Vite's preview. It is built
  // outside the mounted folder, so the PC's own dist/ is never touched.
  webServer: {
    command: `npm run api:types && npx vite build --outDir /tmp/screens-dist --emptyOutDir && npx vite preview --outDir /tmp/screens-dist --host 127.0.0.1 --port ${port} --strictPort`,
    url: `http://127.0.0.1:${port}`,
    reuseExistingServer: false,
    timeout: 180_000,
    stdout: 'ignore',
  },
});
