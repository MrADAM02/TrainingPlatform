<script setup lang="ts">
const { t, locale } = useI18n()
const navigation = useNavigation()

// UDashboardSidebar's `side` prop is not RTL-aware on its own (defaults to "left"
// regardless of document direction) — bind it explicitly to the active locale.
const sidebarSide = computed<'left' | 'right'>(() => (locale.value === 'ar' ? 'right' : 'left'))

function toNavigationMenuItems(items: { labelKey: string, icon: string, to: string }[]) {
  return items.map(item => ({ label: t(item.labelKey), icon: item.icon, to: item.to }))
}
</script>

<template>
  <UDashboardSidebar
    :side="sidebarSide" collapsible resizable :min-size="12" :max-size="18" :default-size="14"
    :ui="{ root: sidebarSide === 'right' ? 'border-s border-default' : undefined }"
  >
    <template #header="{ collapsed }">
      <span v-if="!collapsed" class="font-semibold truncate">{{ t('app.name') }}</span>
      <UDashboardSidebarCollapse class="ms-auto" />
    </template>

    <template #default="{ collapsed }">
      <UNavigationMenu
        :items="toNavigationMenuItems(navigation.main)"
        orientation="vertical" :collapsed="collapsed"
      />

      <template v-if="navigation.trainer.length">
        <USeparator class="my-2" />
        <UNavigationMenu
          :items="toNavigationMenuItems(navigation.trainer)"
          orientation="vertical" :collapsed="collapsed"
        />
      </template>

      <template v-if="navigation.admin.length">
        <USeparator class="my-2" />
        <UNavigationMenu
          :items="toNavigationMenuItems(navigation.admin)"
          orientation="vertical" :collapsed="collapsed"
        />
      </template>
    </template>
  </UDashboardSidebar>
</template>
