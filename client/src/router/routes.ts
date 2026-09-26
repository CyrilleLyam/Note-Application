import type { RouteRecordRaw } from 'vue-router'
import { coreRoutes } from '@/core/routes'
import { authRoutes } from '@/modules/auth/routes'
import { notesRoutes } from '@/modules/notes/routes'

export default [
  ...notesRoutes,
  ...authRoutes,
  ...coreRoutes,
] satisfies RouteRecordRaw[]
