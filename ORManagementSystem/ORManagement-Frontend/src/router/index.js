import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/authStore'

import Login from '../pages/auth/Login.vue'
import Register from '../pages/auth/Register.vue'

import AppLayout from '../layouts/AppLayout.vue'

import SurgeonDashboard from '../pages/surgeon/SurgeonDashboard.vue'
import RequestORTime from '../pages/surgeon/RequestORTime.vue'
import MyRequests from '../pages/surgeon/MyRequests.vue'
import MyBlocks from '../pages/surgeon/MyBlocks.vue'

import SchedulerDashboard from '../pages/scheduler/SchedulerDashboard.vue'
import RoomsCalendar from '../pages/scheduler/RoomsCalendar.vue'
import RequestQueue from '../pages/scheduler/RequestQueue.vue'
import CycleManagement from '../pages/scheduler/CycleManagement.vue'
import CaseManagement from '../pages/scheduler/CaseManagement.vue'
import BlockManagement from '../pages/scheduler/BlockManagement.vue'
import WaitlistBackfill from '../pages/scheduler/WaitlistBackfill.vue'
import UtilizationReports from '../pages/scheduler/UtilizationReports.vue'
import ForecastRecommendations from '../pages/scheduler/ForecastRecommendations.vue'
import SettingsPage from '../pages/scheduler/SettingsPage.vue'
import AuditHistory from '../pages/scheduler/AuditHistory.vue'

import NotificationStack from '../pages/shared/NotificationStack.vue'

const routes = [
  {
    path: '/',
    name: 'login',
    component: Login
  },
  {
    path: '/register',
    name: 'register',
    component: Register
  },
  {
    path: '/app',
    component: AppLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        redirect: () => {
          const authStore = useAuthStore()

          if (authStore.user?.roleName === 'Surgeon') {
            return '/app/surgeon/dashboard'
          }

          if (authStore.user?.roleName === 'ORScheduler') {
            return '/app/scheduler/dashboard'
          }

          return '/'
        }
      },

      {
        path: 'surgeon/dashboard',
        component: SurgeonDashboard,
        meta: { role: 'Surgeon' }
      },
      {
        path: 'surgeon/requests',
        component: MyRequests,
        meta: { role: 'Surgeon' }
      },
      {
        path: 'surgeon/request-or-time',
        component: RequestORTime,
        meta: { role: 'Surgeon' }
      },
      {
        path: 'surgeon/blocks',
        component: MyBlocks,
        meta: { role: 'Surgeon' }
      },
      {
        path: 'scheduler/dashboard',
        component: SchedulerDashboard,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/rooms-calendar',
        component: RoomsCalendar,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/requests',
        component: RequestQueue,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/cycles',
        component: CycleManagement,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/cases',
        component: CaseManagement,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/blocks',
        component: BlockManagement,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/waitlist',
        component: WaitlistBackfill,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/utilization',
        component: UtilizationReports,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/forecast',
        component: ForecastRecommendations,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/settings',
        component: SettingsPage,
        meta: { role: 'ORScheduler' }
      },
      {
        path: 'scheduler/audit',
        component: AuditHistory,
        meta: { role: 'ORScheduler' }
      },

      {
        path: 'notifications',
        component: NotificationStack,
        meta: { requiresAuth: true }
      }
    ]
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/'
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})
router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  if (!authStore.initialized) {
    await authStore.loadUser()
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return '/'
  }

  const requiredRole = to.meta.role

  if (requiredRole && authStore.user?.roleName !== requiredRole) {
    if (authStore.user?.roleName === 'Surgeon') {
      return '/app/surgeon/dashboard'
    }

    if (authStore.user?.roleName === 'ORScheduler') {
      return '/app/scheduler/dashboard'
    }

    return '/'
  }

  if ((to.path === '/' || to.path === '/register') && authStore.isAuthenticated) {
    if (authStore.user?.roleName === 'Surgeon') {
      return '/app/surgeon/dashboard'
    }

    if (authStore.user?.roleName === 'ORScheduler') {
      return '/app/scheduler/dashboard'
    }
  }
})

export default router