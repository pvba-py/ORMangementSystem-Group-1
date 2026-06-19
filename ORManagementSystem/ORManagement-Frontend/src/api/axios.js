import axios from 'axios'

const api = axios.create({
  baseURL: 'https://localhost:7134/api',
  withCredentials: true
})

api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      const url = error.config?.url || ''

      const isAuthEndpoint =
        url.includes('/auth/login') ||
        url.includes('/auth/register') ||
        url.includes('/auth/me')

      if (!isAuthEndpoint) {
        sessionStorage.removeItem('authUser')
      }
    }

    return Promise.reject(error)
  }
)

export default api