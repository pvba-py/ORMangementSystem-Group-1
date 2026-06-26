import api from '../api/axios'

export const getBlockTemplates = () => {
  return api.get('/block-templates')
}

export const createBlockTemplate = (payload) => {
  return api.post('/block-templates', payload)
}

export const updateBlockTemplate = (id, payload) => {
  return api.put(`/block-templates/${id}`, payload)
}


export const addBlockException = (templateId, payload) => {
  return api.post(`/block-templates/${templateId}/exceptions`, payload)
}

export const deleteBlockException = (templateId, exceptionId) => {
  return api.delete(`/block-templates/${templateId}/exceptions/${exceptionId}`)
}

export const generateBlocks = (payload) => {
  return api.post('/blocks/generate', payload)
}

export const getBlocks = (params = {}) => {
  return api.get('/blocks', { params })
}

export const getMyBlocks = () => {
  return api.get('/blocks/my')
}

export const createBlock = (payload) => {
  return api.post('/blocks', payload)
}

export const updateBlock = (id, payload) => {
  return api.put(`/blocks/${id}`, payload)
}

export const cancelBlock = (id) => {
  return api.delete(`/blocks/${id}`)
}

export const deactivateBlockTemplate = id => {
  return api.put(`/block-templates/${id}/deactivate`)
}

export const deleteBlockTemplate = id => {
  return api.delete(`/block-templates/${id}`)
}

export const releaseBlock = (id, payload) => {
  return api.post(`/blocks/${id}/release`, payload)
}