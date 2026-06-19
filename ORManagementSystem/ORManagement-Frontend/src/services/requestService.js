import api from '../api/axios'

export const getRequests = (params = {}) => {
  return api.get('/requests', { params })
}

export const getMyRequests = () => {
  return api.get('/requests/my')
}

export const getRequestById = (id) => {
  return api.get(`/requests/${id}`)
}

export const createRequest = (payload) => {
  return api.post('/requests', payload)
}

export const updateRequest = (id, payload) => {
  return api.put(`/requests/${id}`, payload)
}

export const cancelRequest = (id) => {
  return api.delete(`/requests/${id}`)
}

export const updateRequestStatus = (id, payload) => {
  return api.put(`/requests/${id}/status`, payload)
}

export const getRequestScore = (id) => {
  return api.get(`/requests/${id}/score`)
}