import { readdir, readFile, writeFile } from 'node:fs/promises';
import { extname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { brotliCompressSync, constants, gzipSync } from 'node:zlib';

const root = fileURLToPath(new URL('../dist/AniLink/browser', import.meta.url));
const extensions = new Set(['.css', '.html', '.js', '.json', '.svg', '.txt', '.webmanifest', '.xml']);
const smallest = 1024;

async function* walk(folder) {
  for (const entry of await readdir(folder, { withFileTypes: true })) {
    const path = join(folder, entry.name);

    if (entry.isDirectory()) yield* walk(path);
    else if (extensions.has(extname(entry.name))) yield path;
  }
}

let files = 0;
let raw = 0;
let br = 0;
let gz = 0;

for await (const path of walk(root)) {
  const content = await readFile(path);
  if (content.length < smallest) continue;

  const brotliContent = brotliCompressSync(content, { params: { [constants.BROTLI_PARAM_QUALITY]: 11, [constants.BROTLI_PARAM_SIZE_HINT]: content.length } });
  const gzipContent = gzipSync(content, { level: 9 });

  if (brotliContent.length < content.length) await writeFile(`${path}.br`, brotliContent);
  if (gzipContent.length < content.length) await writeFile(`${path}.gz`, gzipContent);

  files++;
  raw += content.length;
  br += Math.min(brotliContent.length, content.length);
  gz += Math.min(gzipContent.length, content.length);
}

const kib = bytes => `${(bytes / 1024).toFixed(0)} KiB`;

console.log(`Precompressed ${files} files: ${kib(raw)} -> ${kib(br)} with Brotli, ${kib(gz)} with gzip.`);