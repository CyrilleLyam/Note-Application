import type { MessageSchema } from '@/i18n/locales/en'

declare module 'vue-i18n' {
  export interface DefineLocaleMessage extends MessageSchema {}
}
