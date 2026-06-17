import api from '../api/axios'

// Register
export const register = async (data) =>
  await api.post('/auth/register', data)

// Login
export const login = async (data) =>
  await api.post('/auth/login', data)

// Refresh
export const refresh = async () =>
  await api.post('/auth/refresh')

// Get current user
export const getMe = async () =>
  await api.get('/auth/me')

// Logout
export const logout = async () =>
  await api.post('/auth/logout')
