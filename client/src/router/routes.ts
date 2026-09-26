import type { RouteRecordRaw } from 'vue-router'
import { coreRoutes } from '@/core/routes'
import { authRoutes } from '@/modules/auth'
import { notesRoutes } from '@/modules/notes'

export default [
  ...notesRoutes,
  ...authRoutes,
  ...coreRoutes,
] satisfies RouteRecordRaw[]
