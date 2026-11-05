// Renders stills of a composition at the given frames from one bundle.
// Usage: node scripts/stills.mjs <compositionId> <outDir> <scale> <frame> [frame...]
import { bundle } from "@remotion/bundler";
import { renderStill, selectComposition } from "@remotion/renderer";
import path from "node:path";

const [id, outDir, scale, ...frames] = process.argv.slice(2);
const serveUrl = await bundle({ entryPoint: path.resolve("src/index.ts") });
const composition = await selectComposition({ serveUrl, id });
for (const f of frames) {
  const output = path.join(outDir, `${id}-${f}.png`);
  await renderStill({ serveUrl, composition, frame: Number(f), output, scale: Number(scale) });
  console.log(output);
}
