import { useRoute, useRouter } from 'vue-router'

export function useAuthRedirect() {
  const router = useRouter()
  const route = useRoute()

  return () => {
    const { redirect } = route.query
    const target = typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')
      ? redirect
      : '/'
    return router.push(target)
  }
}
