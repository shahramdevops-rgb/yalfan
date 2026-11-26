import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// The API answers on the panel's own origin (ADR 0014): in development Vite forwards the API
// routes to it, in production the reverse proxy does the same. So the panel needs no CORS and
// the refresh cookie (SameSite=Strict, Path /panel/auth) works as it is. Because of this, no
// page of the panel may have a path under /panel/: those requests belong to the API.
const apiUrl = process.env.YALFAN_API_URL ?? 'http://localhost:5080';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/panel': apiUrl,
    },
  },
  test: {
    include: ['src/**/*.test.ts', 'checks/**/*.test.ts'],
    // Far from Tehran on purpose: a formatter that leaned on the machine's time zone instead
    // of Asia/Tehran fails here (ADR 0005).
    env: { TZ: 'America/Los_Angeles' },
  },
});
