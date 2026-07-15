import { apiClient } from './apiClient.js';

export function getBusinessHours() {
  return apiClient.get('/staff/business-hours');
}

export function updateBusinessHours(days) {
  return apiClient.put('/staff/business-hours', { days });
}
