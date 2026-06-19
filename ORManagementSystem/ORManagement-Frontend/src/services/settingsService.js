import api from '../api/axios'

export const getSettings = () => {
  return api.get('/settings')
}

export const getSettingByKey = (key) => {
  return api.get(`/settings/${key}`)
}

export const updateSetting = (key, payload) => {
  return api.put(`/settings/${key}`, payload)
}