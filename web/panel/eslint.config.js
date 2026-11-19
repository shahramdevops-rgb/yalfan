import js from '@eslint/js';
import { defineConfig, globalIgnores } from 'eslint/config';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import globals from 'globals';
import tseslint from 'typescript-eslint';

// Lint for the panel (ADR 0015). `npm run lint` fails on any warning too, as the .NET build does.

// Letters of the Arabic script block, which holds Persian and Persian digits.
const persianText = '/[\\u0600-\\u06FF]/';

// Tailwind utilities tied to the physical left or right. The panel is right-to-left and the store
// will also be left-to-right, so only the logical ones are allowed: ms/me, ps/pe, start/end,
// text-start/end, border-s/e, rounded-s/e, float-start/end, scroll-ms/me...
const physicalDirection =
  '/(^|[\\s:!])-?(m[lr]|p[lr]|scroll-[mp][lr]|left|right)-|(^|[\\s:!])(border-[lr]|rounded-[lr]|rounded-[tb][lr]|text-left|text-right|float-left|float-right|clear-left|clear-right)($|[\\s!-])/';

/** The same check on plain strings and on the text parts of template strings. */
const inStrings = (pattern, message) => [
  { selector: `Literal[value=${pattern}]`, message },
  { selector: `TemplateElement[value.raw=${pattern}]`, message },
];

export default defineConfig([
  // schema.d.ts is generated from openapi/panel.json (npm run api:types).
  globalIgnores(['dist', 'src/shared/api/schema.d.ts']),

  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      // With type information: it also finds a promise nobody awaits, a common bug in event
      // handlers, which the compiler does not report.
      tseslint.configs.recommendedTypeChecked,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname },
    },
  },

  {
    files: ['src/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-syntax': [
        'error',
        ...inStrings(
          physicalDirection,
          'Use the logical Tailwind utility (ms-, me-, ps-, pe-, start-, end-, text-start...).',
        ),
      ],
    },
  },

  {
    // Components hold no text: every word the user reads comes from src/shared/i18n, so it can
    // be found, reviewed and later translated in one place.
    files: ['src/**/*.tsx'],
    rules: {
      'no-restricted-syntax': [
        'error',
        ...inStrings(
          physicalDirection,
          'Use the logical Tailwind utility (ms-, me-, ps-, pe-, start-, end-, text-start...).',
        ),
        { selector: `JSXText[value=${persianText}]`, message: 'Move this text to src/shared/i18n and use t().' },
        ...inStrings(persianText, 'Move this text to src/shared/i18n and use t().'),
      ],
    },
  },

  {
    files: ['**/*.{js,mjs}'],
    extends: [js.configs.recommended],
    languageOptions: { globals: globals.node },
  },
]);
