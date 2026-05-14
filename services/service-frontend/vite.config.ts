import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "path";

// https://vite.dev/config/
export default defineConfig({
    plugins: [react(), tailwindcss()],
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "./src"),
        },
    },
    server: {
        host: true,
        port: 3000,
        strictPort: true,
        watch: {
            usePolling: true, // Enable polling for file changes
        },
    },
    test: {
        environment: "jsdom",
        include: ["src/**/*.test.{ts,tsx}"],
        setupFiles: ["src/test-setup.ts"],
        clearMocks: true,
        restoreMocks: true,
        coverage: {
            reporter: ["text", "json-summary", "json"],
            reportOnFailure: true,
        },
        fileParallelism: true,
        env: {
            VITE_AUTH_API_URL: "http://localhost:5000",
            VITE_ACCOUNT_API_URL: "http://localhost:8000",
        },
    },
});
