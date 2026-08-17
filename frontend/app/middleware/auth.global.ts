// UX-level route gating only. The API enforces every authorization decision server-side
// (REQ-RBAC-01) — this middleware just avoids flashing pages the user can't use.
export default defineNuxtRouteMiddleware((to) => {
  const authStore = useAuthStore()

  if (to.path === '/') {
    return navigateTo(authStore.isAuthenticated ? '/dashboard' : '/auth/login')
  }

  if (to.path === '/auth/login') {
    if (authStore.isAuthenticated) {
      return navigateTo('/dashboard')
    }
    return
  }

  if (!authStore.isAuthenticated) {
    return navigateTo('/auth/login')
  }

  if (to.path.startsWith('/admin') && !authStore.hasRole('Administrator')) {
    return navigateTo('/dashboard')
  }

  if (
    to.path.startsWith('/trainer')
    && !authStore.hasRole('Trainer')
    && !authStore.hasRole('Administrator')
  ) {
    return navigateTo('/dashboard')
  }
})
