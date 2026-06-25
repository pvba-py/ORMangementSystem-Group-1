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

export const calculateUtilization = payload => {
  return api.post('/utilization/calculate', payload)
}

export const getORRoomUtilization = (params = {}) => {
  return api.get('/utilization/or-rooms', { params })
}

export const getORRoomUtilizationSummary = (params = {}) => {
  return api.get('/utilization/or-rooms/summary', { params })
}

export const getUnderutilizedORRooms = (params = {}) => {
  return api.get('/utilization/or-rooms/underutilized', { params })
}

export const calculateORRoomWeeklyUtilization = payload => {
  return api.post('/utilization/or-rooms/calculate', payload)
}

export const generateORRoomWeeklyReport = payload => {
  return api.post('/utilization/or-rooms/report', payload)
}