<template>
  <div class="min-h-screen flex flex-col">
    <AppHeader />

    <main class="flex-1 p-4 sm:p-6 pb-20 sm:pb-6">
      <slot />
    </main>

    <AppMobileTabBar />
  </div>
</template>
