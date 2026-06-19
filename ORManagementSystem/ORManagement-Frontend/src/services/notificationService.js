import api from '../api/axios'

export const getNotificationStack = (take = 20) => {
  return api.get('/notification-stack', {
    params: { take }
  })
}

export const getReceivedNotifications = (take = 20) => {
  return api.get('/notification-stack/received', {
    params: { take }
  })
}

export const getSentNotifications = (take = 20) => {
  return api.get('/notification-stack/sent', {
    params: { take }
  })
}