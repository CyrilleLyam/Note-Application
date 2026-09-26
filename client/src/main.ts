import { createApp } from 'vue'
import { useSettingsStore } from '@/core/store'
import AppLayout from '@/core/views/AppLayout.vue'
import { i18n } from '@/plugins/i18n'
import router from '@/router'
import { pinia } from '@/store'
import App from './App.vue'
import 'vue-sonner/style.css'
import '@/assets/styles/main.css'

const app = createApp(App)
app.component('AppLayout', AppLayout)
app.use(pinia)
app.use(i18n)
useSettingsStore(pinia)
app.use(router)
app.mount('#app')
