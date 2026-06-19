import { defineStore } from 'pinia'
import * as authService from '../services/authService'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: JSON.parse(sessionStorage.getItem('authUser') || 'null'),
    isAuthenticated: !!sessionStorage.getItem('authUser'),
    loading: false,
    initialized: !!sessionStorage.getItem('authUser')
  }),

  getters: {
    isSurgeon: (state) => state.user?.roleName === 'Surgeon',
    isScheduler: (state) => state.user?.roleName === 'ORScheduler'
  },

  actions: {
    async login(data) {
      this.loading = true

      try {
        const res = await authService.login(data)

        const user = res.data?.user || res.data?.data?.user || res.data?.authUser

        if (!user) {
          throw new Error('Login succeeded but user data was not returned.')
        }

        this.user = user
        this.isAuthenticated = true
        this.initialized = true

        sessionStorage.setItem('authUser', JSON.stringify(user))
      } finally {
        this.loading = false
      }
    },

    async register(data) {
      this.loading = true

      try {
        await authService.register(data)
      } finally {
        this.loading = false
      }
    },

    async loadUser() {
      const storedUser = sessionStorage.getItem('authUser')

      if (storedUser) {
        this.user = JSON.parse(storedUser)
        this.isAuthenticated = true
        this.initialized = true
        return
      }

      this.user = null
      this.isAuthenticated = false
      this.initialized = true
    },

    async logout() {
      try {
        await authService.logout()
      } catch {
        // ignore logout API failure
      } finally {
        this.user = null
        this.isAuthenticated = false
        this.initialized = true
        sessionStorage.removeItem('authUser')
      }
    }
  }
})