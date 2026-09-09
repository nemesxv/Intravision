import { defineConfig } from 'vite';
export default defineConfig(({ command }) => ({
  base: command === 'build' ? '/app/' : '/',
  build: { outDir: '../Intravision/wwwroot/app', emptyOutDir: true },
  server: { proxy: { '/api': 'http://localhost:5205', '/images': 'http://localhost:5205' } }
}));
