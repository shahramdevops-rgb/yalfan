import { readdirSync, readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { compile } from 'tailwindcss';
import ts from 'typescript';
import { beforeAll, describe, expect, it } from 'vitest';

// Every colour in the panel comes from the named colours in src/index.css (ADR 0029). Tailwind's
// own palette is switched off there, so a raw colour such as `text-stone-500`, or a misspelt
// name such as `hover:bg-subtel`, makes no CSS and nothing warns: the text simply loses its
// colour. The screenshots would show a lost colour, but not under the pointer or the keyboard's
// focus, which they never picture. This test hands every colour class in src to Tailwind's own
// compiler, as the build does, and fails on each one that makes nothing.

const panel = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const src = join(panel, 'src');
const require = createRequire(import.meta.url);

/**
 * A utility that takes a colour, without its variants. Its other forms (`text-sm`, `ring-1`,
 * `border-e`) make CSS too. An empty rest (`bg-`) is the front of a class built from pieces.
 */
const colourUtility =
  /^!?-?(?:bg|text|border(?:-[xytbse])?|ring(?:-offset)?|inset-ring|outline|divide|fill|stroke|decoration|accent|caret|from|via|to|shadow|inset-shadow|placeholder)-(\S*)$/;

/** A colour written out instead of named: `bg-[#fff]`, `text-[oklch(…)]`, `#fff`, `rgb(…)`. */
const writtenColour = /#[0-9a-f]{3,8}\b|\b(?:rgba?|hsla?|oklch|oklab|color-mix)\(|\[color:/i;

interface Found {
  file: string;
  text: string;
}

/** A class without its variants, which may hold brackets: `data-[state=on]:bg-ink` is `bg-ink`. */
function utilityOf(word: string): string {
  let depth = 0;
  let start = 0;
  for (let index = 0; index < word.length; index++) {
    const character = word[index];
    if (character === '[' || character === '(') {
      depth++;
    } else if (character === ']' || character === ')') {
      depth--;
    } else if (character === ':' && depth === 0) {
      start = index + 1;
    }
  }
  return word.slice(start);
}

/**
 * The colour classes in a text, under any variant, and the fronts of classes built from pieces
 * (`'bg-' + tone`, `` `bg-${tone}` ``), which Tailwind can never find in the code.
 */
function colourWords(text: string): { classes: string[]; pieces: string[] } {
  const classes: string[] = [];
  const pieces: string[] = [];
  for (const word of text.split(/\s+/)) {
    const match = colourUtility.exec(utilityOf(word));
    if (match) {
      (match[1] === '' ? pieces : classes).push(word);
    }
  }
  return { classes, pieces };
}

function sourceFiles(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) {
      return sourceFiles(path);
    }
    const code = /\.tsx?$/.test(entry.name) && !/\.(test|d)\.ts$/.test(entry.name);
    return code ? [path] : [];
  });
}

/** Every string in the code, and every piece of a template string, where class names live. */
function strings(path: string): Found[] {
  const file = relative(panel, path).replaceAll('\\', '/');
  const kind = path.endsWith('.tsx') ? ts.ScriptKind.TSX : ts.ScriptKind.TS;
  const source = ts.createSourceFile(path, readFileSync(path, 'utf8'), ts.ScriptTarget.Latest, true, kind);
  const found: Found[] = [];

  const visit = (node: ts.Node) => {
    if (
      ts.isStringLiteral(node) ||
      ts.isNoSubstitutionTemplateLiteral(node) ||
      ts.isTemplateHead(node) ||
      ts.isTemplateMiddle(node) ||
      ts.isTemplateTail(node)
    ) {
      found.push({ file, text: node.text });
    }
    ts.forEachChild(node, visit);
  };
  visit(source);
  return found;
}

/** A class name as it appears in a CSS selector: `hover:bg-ink/40` is `.hover\:bg-ink\/40`. */
function selectorOf(name: string): RegExp {
  const escaped = name.replace(/[^\w-]/g, (character) => `\\${character}`);
  return new RegExp(`\\.${escaped.replace(/[\\^$.*+?()[\]{}|]/g, '\\$&')}(?![\\w\\\\-])`);
}

let build: (candidates: string[]) => string;

beforeAll(async () => {
  const loadStylesheet = (id: string, base: string) => {
    const path = id === 'tailwindcss' ? require.resolve('tailwindcss/index.css') : resolve(base, id);
    return Promise.resolve({ path, base: dirname(path), content: readFileSync(path, 'utf8') });
  };
  const css = readFileSync(join(src, 'index.css'), 'utf8');
  const compiler = await compile(css, { base: src, loadStylesheet });
  build = (candidates) => compiler.build(candidates);
});

describe('colours', () => {
  it('Tailwind makes CSS for a named colour, and none for its palette or a misspelt name', () => {
    const css = build(['bg-subtle', 'text-stone-500', 'hover:bg-subtel']);

    expect(css).toMatch(selectorOf('bg-subtle'));
    expect(css).not.toMatch(selectorOf('text-stone-500'));
    expect(css).not.toMatch(selectorOf('hover:bg-subtel'));
  });

  it('finds a colour class under any variant, and a class built from pieces', () => {
    const { classes, pieces } = colourWords(
      'px-2 text-sm data-[state=on]:bg-stone-500 aria-[checked=true]:text-red-700 [&>p]:text-muted @md:ring-line bg-',
    );

    expect(classes).toEqual([
      'text-sm',
      'data-[state=on]:bg-stone-500',
      'aria-[checked=true]:text-red-700',
      '[&>p]:text-muted',
      '@md:ring-line',
    ]);
    expect(pieces).toEqual(['bg-']);
  });

  it('every colour class in src is one of the named colours of index.css', () => {
    const found = sourceFiles(src).flatMap(strings);
    const problems: string[] = [];
    const classes = new Map<string, string>();

    for (const { file, text } of found) {
      if (writtenColour.test(text)) {
        problems.push(`${file}: a colour written out, «${text.trim()}»: use a named colour from src/index.css`);
      }
      const words = colourWords(text);
      for (const piece of words.pieces) {
        problems.push(`${file}: «${piece}…» builds a class from pieces, which Tailwind never finds: write it in full`);
      }
      for (const word of words.classes) {
        if (!classes.has(word)) {
          classes.set(word, file);
        }
      }
    }

    const css = build([...classes.keys()]);
    for (const [name, file] of classes) {
      if (!selectorOf(name).test(css)) {
        problems.push(`${file}: «${name}» makes no CSS: use a named colour from src/index.css (docs/design/UI.md)`);
      }
    }

    expect(problems).toEqual([]);
    // The scan reads the code at all: the panel has some eighty different colour classes.
    expect(classes.size).toBeGreaterThan(50);
  });
});
