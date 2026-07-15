import { apiClient } from './apiClient.js';

export function getWeeklyPerformance(referenceDate) {
  const query = referenceDate ? `?referenceDate=${referenceDate}` : '';
  return apiClient.get(`/staff/dashboard/weekly-performance${query}`);
}
