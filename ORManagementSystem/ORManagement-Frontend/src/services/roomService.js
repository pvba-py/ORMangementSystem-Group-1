import api from '../api/axios'

export const getRooms = () => {
  return api.get('/rooms')
}


export const getRoomsPaged = (params = {}) => {
  return api.get('/rooms/paged', { params })
}

export const createRoom = (payload) => {
  return api.post('/rooms', payload)
}

export const updateRoom = (id, payload) => {
  return api.put(`/rooms/${id}`, payload)
}

export const deleteRoom = (id) => {
  return api.delete(`/rooms/${id}`)
}

export const getCalendar = (params = {}) => {
  return api.get('/calendar', { params })
}
export const getMyCalendar = (params = {}) => {
  return api.get('/calendar/my', { params })
}