import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const pkg = JSON.parse(readFileSync(new URL('./package.json', import.meta.url), 'utf-8')) as {
	version?: string;
};

export default defineConfig({
	plugins: [
		react({
			// Enable React Compiler optimizations in development
			babel: {
				plugins: [
					// Babel plugin for automatic React runtime
				],
			},
		}),
	],
	server: {
		port: 65000,
		strictPort: true,
	},
	resolve: {
		alias: {
			'@': fileURLToPath(new URL('./src', import.meta.url)),
		},
	},
	define: {
		__APP_VERSION__: JSON.stringify(pkg.version ?? '0.0.0'),
	},
	build: {
		// Optimize chunk splitting for better caching
		rollupOptions: {
			output: {
				manualChunks: (id) => {
					if (id.includes('node_modules')) {
						if (id.includes('react') || id.includes('react-dom')) {
							return 'vendor-react';
						}
						// Group other small dependencies into a single vendor chunk to avoid too many requests
						return 'vendor';
					}
				},
			},
		},
		// Enable minification with terser for smaller bundles
		minify: 'esbuild',
		// Target modern browsers for smaller output
		target: 'esnext',
		// Enable source maps for debugging (disable in production if not needed)
		sourcemap: false,
		// Reduce chunk size warnings threshold
		chunkSizeWarningLimit: 500,
	},
	// Optimize dependencies
	optimizeDeps: {
		include: ['react', 'react-dom'],
	},
	test: {
		environment: 'jsdom',
		setupFiles: 'test/setup.ts',
		globals: true,
	},
});
