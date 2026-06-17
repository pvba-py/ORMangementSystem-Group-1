import { useToast } from 'vue-toastification'

// create a simple wrapper
export const showToast = (message, type = 'info') => {
  const toast = useToast()

  switch (type) {
    case 'success':
      toast.success(message)
      break
    case 'error':
      toast.error(message)
      break
    case 'warning':
      toast.warning(message)
      break
    default:
      toast.info(message)
      break
  }
}