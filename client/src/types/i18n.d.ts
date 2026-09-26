import type { MessageSchema } from '@/plugins/i18n/locales/en'

declare module 'vue-i18n' {
  export interface DefineLocaleMessage extends MessageSchema {}
}
