import type { RouteRecordRaw } from 'vue-router'

export const notesRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    component: () => import('@/modules/notes/views/NotesPage.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/s/:token',
    name: 'shared-note',
    component: () => import('@/modules/notes/views/SharedNotePage.vue'),
  },
]

export default notesRoutes
