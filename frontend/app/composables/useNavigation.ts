export interface AppNavItem {
  labelKey: string
  icon: string
  to: string
}

export interface AppNavGroups {
  main: AppNavItem[]
  trainer: AppNavItem[]
  admin: AppNavItem[]
}

/**
 * Single source of truth for sidebar navigation, replacing the ad hoc
 * `authStore.hasRole(...)` chain that used to live inline in the layout.
 */
export function useNavigation(): ComputedRef<AppNavGroups> {
  const authStore = useAuthStore()

  return computed(() => ({
    main: [
      { labelKey: 'nav.dashboard', icon: 'i-lucide-layout-dashboard', to: '/dashboard' },
      { labelKey: 'nav.courses', icon: 'i-lucide-book-open', to: '/courses' },
      { labelKey: 'nav.search', icon: 'i-lucide-search', to: '/search' },
    ],
    trainer: authStore.hasRole('Trainer') || authStore.hasRole('Administrator')
      ? [{ labelKey: 'nav.trainer', icon: 'i-lucide-graduation-cap', to: '/trainer' }]
      : [],
    admin: authStore.hasRole('Administrator')
      ? [{ labelKey: 'nav.admin', icon: 'i-lucide-shield', to: '/admin' }]
      : [],
  }))
}
