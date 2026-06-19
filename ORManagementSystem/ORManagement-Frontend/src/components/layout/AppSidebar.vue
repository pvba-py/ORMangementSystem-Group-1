<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../../stores/authStore'

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
  },
  {
    label: 'My Cases',
    icon: 'bi-hospital',
    to: '/app/surgeon/cases'
  }
]

const schedulerLinks = [
  {
    label: 'Dashboard',
    icon: 'bi-speedometer2',
    to: '/app/scheduler/dashboard'
  },
  {
    label: 'Rooms & Calendar',
    icon: 'bi-calendar3',
    to: '/app/scheduler/rooms-calendar'
  },
  {
    label: 'Request Queue',
    icon: 'bi-inbox',
    to: '/app/scheduler/requests'
  },
  {
    label: 'Cycles',
    icon: 'bi-arrow-repeat',
    to: '/app/scheduler/cycles'
  },
  {
    label: 'Cases',
    icon: 'bi-clipboard2-pulse',
    to: '/app/scheduler/cases'
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
  <aside class="app-sidebar">
    <div class="brand">
      <i class="bi bi-hospital"></i>
      <span>OR Manager</span>
    </div>

    <nav class="sidebar-nav">
      <router-link
        v-for="link in links"
        :key="link.to"
        :to="link.to"
        class="sidebar-link"
        active-class="active"
      >
        <i :class="`bi ${link.icon}`"></i>
        <span>{{ link.label }}</span>
      </router-link>
    </nav>
  </aside>
</template>

<style scoped>
.app-sidebar {
  width: 260px;
  background: #111827;
  color: white;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.brand {
  height: 64px;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 0 20px;
  font-size: 18px;
  font-weight: 700;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
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
}

.sidebar-link:hover {
  background: rgba(255, 255, 255, 0.08);
  color: white;
}

.sidebar-link.active {
  background: #2563eb;
  color: white;
}
</style>