import { createPinia } from 'pinia'
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import { createApp } from 'vue'
import { i18n } from '@/i18n'
import AppLayout from '@/layouts/AppLayout.vue'
import router from '@/router/index'
import { useSettingsStore } from '@/stores/settings'
import App from './App.vue'
import 'vue-sonner/style.css'
import './style.css'

const pinia = createPinia()
pinia.use(piniaPluginPersistedstate)

const app = createApp(App)
app.component('AppLayout', AppLayout)
app.use(pinia)
app.use(i18n)
useSettingsStore(pinia)
app.use(router)
app.mount('#app')
