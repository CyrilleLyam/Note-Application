import type { RouteRecordRaw } from 'vue-router'

export const coreRoutes: RouteRecordRaw[] = [
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/core/views/NotFoundPage.vue'),
    meta: { titleKey: 'notFound.title' },
  },
]

export default coreRoutes
