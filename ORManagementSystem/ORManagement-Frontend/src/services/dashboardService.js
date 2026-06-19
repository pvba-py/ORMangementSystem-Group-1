import api from '../api/axios'

export const getSurgeonDashboard = () => {
  return api.get('/dashboard/surgeon')
}

export const getSchedulerDashboard = () => {
  return api.get('/dashboard/scheduler')
}

export const getTodaySchedule = (date = null) => {
  return api.get('/dashboard/today', {
    params: { date }
  })
}