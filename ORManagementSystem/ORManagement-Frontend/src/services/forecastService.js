import api from '../api/axios'

export const getForecastSummary = () => {
  return api.get('/forecast/summary')
}