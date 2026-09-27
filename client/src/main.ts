import * as Sentry from '@sentry/vue'
import { createApp, watch } from 'vue'
import { useSettingsStore } from '@/core/store'
import AppLayout from '@/core/views/AppLayout.vue'
import { useAuthStore } from '@/modules/auth'
import { i18n } from '@/plugins/i18n'
import router from '@/router'
import { pinia } from '@/store'
import App from './App.vue'
import 'vue-sonner/style.css'
import '@/assets/styles/main.css'

const app = createApp(App)
const glitchTipDsn = import.meta.env.VITE_GLITCHTIP_DSN

if (glitchTipDsn) {
  Sentry.init({
    app,
    dsn: glitchTipDsn,
    environment: import.meta.env.MODE,
    integrations: [Sentry.browserTracingIntegration({ router })],
    tracesSampleRate: import.meta.env.DEV ? 1.0 : 0.2,
    traceLifecycle: 'static',
  })
}

app.component('AppLayout', AppLayout)
app.use(pinia)
app.use(i18n)
useSettingsStore(pinia)
app.use(router)

if (glitchTipDsn) {
  const auth = useAuthStore(pinia)
  watch(() => auth.user, (user) => {
    Sentry.setUser(user ? { id: String(user.id), username: user.username } : null)
  }, { immediate: true })
}

app.mount('#app')
