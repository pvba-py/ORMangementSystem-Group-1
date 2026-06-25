import api from '../api/axios'

export const getCurrentCycle = () => {
  return api.get('/cycles/current')
}

export const getRankedRequests = (cycleId) => {
  return api.get(`/cycles/${cycleId}/ranked-requests`)
}
export const getCycles = () => {
  return api.get('/cycles')
}
export const cutoffCycle = (cycleId) => {
  return api.put(`/cycles/${cycleId}/cutoff`)
}

export const publishCycle = (cycleId) => {
  return api.put(`/cycles/${cycleId}/publish`)
}