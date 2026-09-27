import { defineConfig } from 'vite'

// Baut die Web-Component als eine einzige ES-Modul-Datei nach docs/widgets/turntable/v1.js
// (GitHub Pages → https://audiola.de/widgets/turntable/v1.js). Brechende Änderungen → v2.js.
export default defineConfig({
  esbuild: { jsx: 'automatic', jsxImportSource: 'preact' },
  build: {
    outDir: '../../docs/widgets/turntable',
    emptyOutDir: false,
    target: 'es2022',
    lib: { entry: 'src/index.tsx', formats: ['es'], fileName: () => 'v1.js' },
    rollupOptions: { output: { inlineDynamicImports: true } },
  },
})
