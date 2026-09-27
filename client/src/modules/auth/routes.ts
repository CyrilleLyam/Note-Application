import type { RouteRecordRaw } from 'vue-router'

export const authRoutes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/modules/auth/views/LoginPage.vue'),
    meta: { guestOnly: true, titleKey: 'auth.signInTitle' },
  },
  {
    path: '/register',
    name: 'register',
    component: () => import('@/modules/auth/views/RegisterPage.vue'),
    meta: { guestOnly: true, titleKey: 'auth.registerTitle' },
  },
]

export default authRoutes
