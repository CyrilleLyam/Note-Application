import type { RouteRecordRaw } from 'vue-router'

export const notesRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    component: () => import('@/modules/notes/views/NotesPage.vue'),
    meta: { requiresAuth: true },
  },
]

export default notesRoutes
