import api from '../api/axios'

export const getHospitals = () => {
  return api.get('/hospitals')
}

export const getUsers = () => {
  return api.get('/users')
}

export const getSurgeons = () => {
  return api.get('/surgeons')
}

export const getPatients = (search = '') => {
  return api.get('/patients', {
    params: { search }
  })
}

export const getPatientById = (id) => {
  return api.get(`/patients/${id}`)
}