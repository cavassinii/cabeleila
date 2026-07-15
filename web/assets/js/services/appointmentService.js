import { apiClient } from './apiClient.js';

export function suggestDate(dateIsoString) {
  return apiClient.get(`/appointments/suggest-date?date=${dateIsoString}`);
}

export function createAppointment(payload) {
  return apiClient.post('/appointments', payload);
}

export function getHistory(from, to) {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  const query = params.toString() ? `?${params.toString()}` : '';
  return apiClient.get(`/appointments${query}`);
}

export function getById(id) {
  return apiClient.get(`/appointments/${id}`);
}

export function reschedule(id, payload) {
  return apiClient.put(`/appointments/${id}/reschedule`, payload);
}

export function cancel(id) {
  return apiClient.put(`/appointments/${id}/cancel`);
}

export function editServices(id, serviceIds) {
  return apiClient.put(`/appointments/${id}/services`, { serviceIds });
}

export function getAvailability(dateIsoString, durationMinutes, excludeAppointmentId) {
  const params = new URLSearchParams({ date: dateIsoString, durationMinutes: String(durationMinutes) });
  if (excludeAppointmentId) params.set('excludeAppointmentId', String(excludeAppointmentId));
  return apiClient.get(`/appointments/availability?${params.toString()}`);
}
