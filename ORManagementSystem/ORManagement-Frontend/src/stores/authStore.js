import { defineStore } from 'pinia'
import * as authService from '../services/authService'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
    isAuthenticated: false,
    loading: false
  }),

  actions: {
    login: async (data) => {
      try {
        await authService.login(data)
        await this.loadUser()
      } catch (err) {
        throw err
      }
    },

    register: async (data) => {
      try {
        await authService.register(data)
        await this.loadUser()
      } catch (err) {
        throw err
      }
    },

    loadUser: async () => {
      try {
        const res = await authService.getMe()
        this.user = res.data
        this.isAuthenticated = true
      } catch {
        this.user = null
        this.isAuthenticated = false
      }
    },

    logout: async () => {
      await authService.logout()
      this.user = null
      this.isAuthenticated = false
    }
  }
})