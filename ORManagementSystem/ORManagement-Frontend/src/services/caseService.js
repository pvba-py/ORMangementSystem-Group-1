import api from '../api/axios'

export const getCases = (params = {}) => {
  return api.get('/cases', { params })
}

export const getMyCases = () => {
  return api.get('/cases/my')
}

export const getCaseById = (id) => {
  return api.get(`/cases/${id}`)
}

export const createCase = (payload) => {
  return api.post('/cases', payload)
}

export const updateCase = (id, payload) => {
  return api.put(`/cases/${id}`, payload)
}

export const updateCaseStatus = (id, payload) => {
  return api.put(`/cases/${id}/status`, payload)
}