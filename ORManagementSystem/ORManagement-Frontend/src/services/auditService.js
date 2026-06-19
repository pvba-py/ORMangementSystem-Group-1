import api from '../api/axios'

export const getAuditLogs = (params = {}) => {
  return api.get('/audit/logs', { params })
}

export const getPhiAccessLogs = (params = {}) => {
  return api.get('/audit/phi-access-logs', { params })
}