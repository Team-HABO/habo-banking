import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 3000,
    strictPort: true,
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.ts'],
    clearMocks: true,
    restoreMocks: true,
    coverage: {
			reporter: ["text", "json-summary", "json"],
			reportOnFailure: true
		},
		fileParallelism: true,
    env: {
      VITE_AUTH_API_URL: 'http://localhost:5000',
    },
  },
})
