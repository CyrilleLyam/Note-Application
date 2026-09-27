/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
  readonly VITE_API_URL?: string
  readonly VITE_APP_TITLE: string
  readonly VITE_GLITCHTIP_DSN?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
