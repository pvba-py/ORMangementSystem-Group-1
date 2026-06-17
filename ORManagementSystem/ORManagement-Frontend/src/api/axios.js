import axios from 'axios'
import { refresh } from '../services/authService'
import { showToast } from '../utils/toast'

// axios instance
export const api = axios.create({
  baseURL: 'https://localhost:7134/api',
  withCredentials: true
})

// REQUEST INTERCEPTOR
api.interceptors.request.use(
  (config) => {
    // log request (optional, useful for debugging)
    console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`)

    return config
  },
  (error) => {
    showToast('Request failed to send', 'error')
    return Promise.reject(error)
  }
)

// RESPONSE INTERCEPTOR

// prevent multiple refresh calls
let isRefreshing = false
let queue = []

const processQueue = (error) => {
  queue.forEach((promise) => {
    if (error) {
      promise.reject(error)
    } else {
      promise.resolve()
    }
  })
  queue = []
}

api.interceptors.response.use(
  (response) => {
    return response
  },

  async (error) => {
    const originalRequest = error.config
    const status = error.response?.status

    // 401 → try refresh token
    if (status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          queue.push({ resolve, reject })
        })
          .then(() => api(originalRequest))
          .catch((err) => Promise.reject(err))
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        await refresh()

        processQueue(null)

        return api(originalRequest)
      } catch (err) {
        processQueue(err)

        showToast('Session expired. Please login again.', 'warning')
        window.location.href = '/login'

        return Promise.reject(err)
      } finally {
        isRefreshing = false
      }
    }

    // 400 → validation / bad request
    if (status === 400) {
      const message = error.response?.data?.message || 'Invalid request'
      showToast(message, 'error')
    }

    // 403 → forbidden
    if (status === 403) {
      showToast('You do not have permission to perform this action.', 'error')
    }

    // 404 → not found
    if (status === 404) {
      showToast('Requested resource not found.', 'warning')
    }

    // 500+ → server error
    if (status >= 500) {
      showToast('Server error. Please try again later.', 'error')
    }

    return Promise.reject(error)
  }
)

export default api