import { apiClient } from './apiClient.js';

export function listAppointments({ date, status } = {}) {
  const params = new URLSearchParams();
  if (date) params.set('date', date);
  if (status) params.set('status', status);
  const query = params.toString() ? `?${params.toString()}` : '';
  return apiClient.get(`/staff/appointments${query}`);
}

export function getById(id) {
  return apiClient.get(`/staff/appointments/${id}`);
}

export function getHistory(id) {
  return apiClient.get(`/staff/appointments/${id}/history`);
}

export function confirm(id) {
  return apiClient.put(`/staff/appointments/${id}/confirm`);
}

export function reschedule(id, payload) {
  return apiClient.put(`/staff/appointments/${id}/reschedule`, payload);
}

export function cancel(id) {
  return apiClient.put(`/staff/appointments/${id}/cancel`);
}

export function updateItemStatus(itemId, statusCode) {
  return apiClient.put(`/staff/appointments/items/${itemId}/status`, { statusCode });
}

export function editServices(id, serviceIds) {
  return apiClient.put(`/staff/appointments/${id}/services`, { serviceIds });
}

export function getAvailability(dateIsoString, durationMinutes, excludeAppointmentId) {
  const params = new URLSearchParams({ date: dateIsoString, durationMinutes: String(durationMinutes) });
  if (excludeAppointmentId) params.set('excludeAppointmentId', String(excludeAppointmentId));
  return apiClient.get(`/staff/appointments/availability?${params.toString()}`);
}
