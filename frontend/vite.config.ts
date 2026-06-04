import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig({
  envPrefix: "REACT_",
  server: {
    port: parseInt(process.env.PORT || "8080"),
  },
  preview: {
    port: parseInt(process.env.PORT || "8080"),
  },
  resolve: {
    alias: [{ find: "@", replacement: "/src" }],
  },
  plugins: [react()],
  css: {
    preprocessorOptions: {
      scss: {
        quietDeps: true,
      },
    },
  },
});
