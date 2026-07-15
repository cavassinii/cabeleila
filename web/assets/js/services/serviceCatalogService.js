import { apiClient } from './apiClient.js';

export function listActiveServices() {
  return apiClient.get('/services', { auth: false });
}

export function createService(payload) {
  return apiClient.post('/services', payload);
}

export function updateService(id, payload) {
  return apiClient.put(`/services/${id}`, payload);
}

export function deactivateService(id) {
  return apiClient.del(`/services/${id}`);
}
