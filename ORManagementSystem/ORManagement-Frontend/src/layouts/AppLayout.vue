<script setup>
import { ref } from 'vue'
import AppNavbar from '../components/layout/AppNavbar.vue'
import AppSidebar from '../components/layout/AppSidebar.vue'

const sidebarCollapsed = ref(false)

const toggleSidebar = () => {
  sidebarCollapsed.value = !sidebarCollapsed.value
}
</script>

<template>
  <div
    class="app-shell"
    :class="{ 'sidebar-collapsed': sidebarCollapsed }"
  >
    <AppSidebar :collapsed="sidebarCollapsed" />

    <div class="app-main">
      <AppNavbar
        :sidebar-collapsed="sidebarCollapsed"
        @toggle-sidebar="toggleSidebar"
      />

      <main class="app-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<style scoped>
.app-shell {
  display: flex;
  min-height: 100vh;
  background: #f5f7fb;
}

.app-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  transition: all 0.2s ease;
}

.app-content {
  padding: 24px;
}
</style>