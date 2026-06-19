import api from '../api/axios'

export const getReleasedSlots = (params = {}) => {
  return api.get('/released-slots', { params })
}

export const getSlotMatches = (slotId) => {
  return api.get(`/released-slots/${slotId}/matches`)
}

export const updateReleasedSlotStatus = (slotId, payload) => {
  return api.put(`/released-slots/${slotId}/status`, payload)
}

export const getWaitlist = () => {
  return api.get('/waitlist')
}

export const assignWaitlist = (waitlistId, payload) => {
  return api.put(`/waitlist/${waitlistId}/assign`, payload)
}

export const removeWaitlist = (waitlistId) => {
  return api.delete(`/waitlist/${waitlistId}`)
}

export const getStarvationList = () => {
  return api.get('/waitlist/starvation')
}