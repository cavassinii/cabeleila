import { formatDate, formatCurrency } from '../models/Appointment.js';

export function renderPerformance(performance) {
  document.getElementById('week-range').textContent = `Semana de ${formatDate(performance.weekStart)} a ${formatDate(performance.weekEnd)}`;
  document.getElementById('kpi-total').textContent = performance.totalAppointments;
  document.getElementById('kpi-completed').textContent = performance.completedAppointments;
  document.getElementById('kpi-cancelled').textContent = performance.cancelledAppointments;
  document.getElementById('kpi-revenue').textContent = formatCurrency(performance.totalRevenue);
}
