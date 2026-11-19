// Generates src/shared/api/schema.d.ts from openapi/panel.json (ADR 0014).
// It calls openapi-typescript through its API with a proper file URL, because the CLI builds
// the URL from the working directory without escaping it and breaks on a "#" in the path.
import { writeFile } from 'node:fs/promises';
import openapiTS, { astToString } from 'openapi-typescript';

const source = new URL('../openapi/panel.json', import.meta.url);
const target = new URL('../src/shared/api/schema.d.ts', import.meta.url);

const banner =
  '// Generated from openapi/panel.json by scripts/generate-api-types.mjs. Do not edit;\n' +
  '// the contract itself is kept equal to the API by PanelDocumentTests.\n\n';

const ast = await openapiTS(source);
await writeFile(target, banner + astToString(ast));
