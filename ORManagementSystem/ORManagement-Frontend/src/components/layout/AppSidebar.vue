<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../../stores/authStore'
import logoUrl from '../../assets/images/image.png'

defineProps({
  collapsed: {
    type: Boolean,
    default: false
  }
})

const authStore = useAuthStore()

const surgeonLinks = [
  {
    label: 'Dashboard',
    icon: 'bi-speedometer2',
    to: '/app/surgeon/dashboard'
  },
  {
    label: 'My Requests',
    icon: 'bi-list-check',
    to: '/app/surgeon/requests'
  },
  {
    label: 'Request OR Time',
    icon: 'bi-plus-circle',
    to: '/app/surgeon/request-or-time'
  },
  {
    label: 'My Blocks',
    icon: 'bi-calendar-week',
    to: '/app/surgeon/blocks'
  }
]

const schedulerLinks = [
  {
    label: 'Dashboard',
    icon: 'bi-speedometer2',
    to: '/app/scheduler/dashboard'
  },
  {
    label: 'OR Rooms',
    icon: 'bi-calendar3',
    to: '/app/scheduler/rooms-calendar'
  },
  {
    label: 'Request Approval',
    icon: 'bi-inbox',
    to: '/app/scheduler/requests'
  },
  {
    label: 'Cycle Calendar',
    icon: 'bi-arrow-repeat',
    to: '/app/scheduler/cycles'
  },
  {
    label: 'Blocks',
    icon: 'bi-grid-3x3-gap',
    to: '/app/scheduler/blocks'
  },
  {
    label: 'Waitlist & Backfill',
    icon: 'bi-hourglass-split',
    to: '/app/scheduler/waitlist'
  },
  {
    label: 'Utilization',
    icon: 'bi-bar-chart',
    to: '/app/scheduler/utilization'
  },
  {
    label: 'Forecast',
    icon: 'bi-graph-up-arrow',
    to: '/app/scheduler/forecast'
  },
  {
    label: 'Settings',
    icon: 'bi-gear',
    to: '/app/scheduler/settings'
  },
  {
    label: 'Audit History',
    icon: 'bi-shield-check',
    to: '/app/scheduler/audit'
  }
]

const links = computed(() => {
  if (authStore.user?.roleName === 'Surgeon') {
    return surgeonLinks
  }

  if (authStore.user?.roleName === 'ORScheduler') {
    return schedulerLinks
  }

  return []
})
</script>

<template>
  <aside
    class="app-sidebar"
    :class="{ collapsed }"
  >
    <div class="brand">
      <img
        :src="logoUrl"
        alt="HCA Healthcare"
        class="brand-logo"
        :class="{ 'brand-logo-collapsed': collapsed }"
      />
    </div>

    <nav class="sidebar-nav">
      <router-link
        v-for="link in links"
        :key="link.to"
        :to="link.to"
        class="sidebar-link"
        active-class="active"
        :title="collapsed ? link.label : ''"
      >
        <i :class="`bi ${link.icon}`"></i>

        <span
          v-if="!collapsed"
          class="sidebar-label"
        >
          {{ link.label }}
        </span>
      </router-link>
    </nav>
  </aside>
</template>

<style scoped>
.app-sidebar {
  width: 260px;
  background: #071f49;
  color: white;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  transition: width 0.2s ease;
  overflow-x: hidden;
}

.app-sidebar.collapsed {
  width: 76px;
}
.brand {
  height: 76px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px 10px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
}

.brand-logo {
  max-width: 180px;
  width: 100%;
  height: 80px;
  object-fit: contain;
  transition: all 0.2s ease;
}

.brand-logo-collapsed {
  max-width: 48px;
}

.sidebar-nav {
  padding: 16px 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.sidebar-link {
  color: #d1d5db;
  text-decoration: none;
  padding: 11px 14px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 14px;
  white-space: nowrap;
  transition: background 0.15s ease, color 0.15s ease;
}

.sidebar-link i {
  font-size: 18px;
  min-width: 20px;
  text-align: center;
}

.app-sidebar.collapsed .sidebar-link {
  justify-content: center;
  padding: 11px 0;
}

.sidebar-link:hover {
  background: rgba(255, 255, 255, 0.08);
  color: white;
}

.sidebar-link.active {
  background: #2563eb;
  color: white;
}

.sidebar-label {
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>