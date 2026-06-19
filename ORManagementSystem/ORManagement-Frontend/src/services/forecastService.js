import api from '../api/axios'

export const getForecastDemand = () => {
  return api.get('/forecast/demand')
}

export const getForecastRecommendations = (params = {}) => {
  return api.get('/forecast/recommendations', { params })
}

export const generateForecastRecommendations = () => {
  return api.post('/forecast/generate')
}

export const updateForecastStatus = (id, payload) => {
  return api.put(`/forecast/recommendations/${id}/status`, payload)
}