import type { RouteRecordRaw } from 'vue-router'

export const notesRoutes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    component: () => import('@/modules/notes/views/NotesPage.vue'),
    meta: { requiresAuth: true, titleKey: 'notes.pageTitle' },
  },
  {
    path: '/s/:token',
    name: 'shared-note',
    component: () => import('@/modules/notes/views/SharedNotePage.vue'),
    meta: { titleKey: 'sharedNote.pageTitle' },
  },
]

export default notesRoutes
