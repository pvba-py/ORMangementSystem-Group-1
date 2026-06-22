<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/authStore'
import { showToast } from '../../utils/toast'

defineProps({
  sidebarCollapsed: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['toggle-sidebar'])

const router = useRouter()
const authStore = useAuthStore()

const handleLogout = async () => {
  try {
    await authStore.logout()

    showToast('Logged out successfully', 'success')

    router.replace('/')
  } catch {
    showToast('Logout failed', 'error')
  }
}
</script>

<template>
  <nav class="app-navbar">
    <div class="navbar-left">
      <button
        class="btn btn-outline-secondary btn-sm sidebar-toggle"
        @click="emit('toggle-sidebar')"
      >
        <i
          class="bi"
          :class="sidebarCollapsed ? 'bi-list' : 'bi-layout-sidebar-inset'"
        ></i>
      </button>

      <div>
        <h5 class="mb-0">OR Block Schedule Management</h5>
        <small class="text-muted">
          {{ authStore.user?.roleName || 'User' }}
        </small>
      </div>
    </div>

    <div class="d-flex align-items-center gap-3">
      <router-link class="notification-link" to="/app/notifications">
        <i class="bi bi-bell"></i>
      </router-link>

      <div class="user-info d-none d-md-flex">
        <strong>{{ authStore.user?.fullName }}</strong>
        <small>{{ authStore.user?.email }}</small>
      </div>

      <button class="btn btn-outline-danger btn-sm" @click="handleLogout">
        <i class="bi bi-box-arrow-right me-1"></i>
        Logout
      </button>
    </div>
  </nav>
</template>

<style scoped>
.app-navbar {
  height: 64px;
  background: white;
  border-bottom: 1px solid #e5e7eb;
  padding: 0 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.navbar-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.sidebar-toggle {
  width: 36px;
  height: 32px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.user-info {
  flex-direction: column;
  font-size: 13px;
  line-height: 1.2;
}

.notification-link {
  color: #111827;
  font-size: 20px;
  text-decoration: none;
}

.notification-link:hover {
  color: #2563eb;
}
</style>