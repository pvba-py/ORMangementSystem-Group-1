import api from '../api/axios'

export const getUtilization = (params = {}) => {
  return api.get('/utilization', { params })
}

export const getUtilizationSummary = (params = {}) => {
  return api.get('/utilization/summary', { params })
}

export const getUnderutilizedBlocks = (params = {}) => {
  return api.get('/utilization/underutilized', { params })
}

export const calculateUtilization = (payload) => {
  return api.post('/utilization/calculate', payload)
}