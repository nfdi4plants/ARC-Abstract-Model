import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";

const repository = fileURLToPath(new URL(".", import.meta.url));
const libraries = {
  "arc-base-model": { directory: "base-model", external: [] as string[] },
  "polyglot-sqlite": { directory: "polyglot-sqlite", external: ["better-sqlite3"] },
};

// The native staging scripts select a library mode after Fable compilation and
// supply additional test entries/plugins when running the shared consumer tests.
export default defineConfig(({ mode }) => {
  const library = libraries[mode as keyof typeof libraries];
  if (!library) {
    throw new Error("Use npm run build to transpile and build both libraries, or select a library build script.");
  }
  const output = path.join(repository, "build/out", library.directory);
  return {
    root: repository,
    publicDir: false,
    build: {
      outDir: path.join(output, "js"),
      emptyOutDir: true,
      minify: false,
      sourcemap: true,
      target: "es2022",
      rollupOptions: {
        input: { [`node_modules/${mode}/index`]: path.join(output, "ts/index.ts") },
        external: library.external,
        preserveEntrySignatures: "strict",
        output: {
          format: "es",
          entryFileNames: "[name].js",
          chunkFileNames: `node_modules/${mode}/chunks/[name]-[hash].js`,
        },
      },
    },
  };
});
