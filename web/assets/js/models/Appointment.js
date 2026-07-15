// Espelha API.BusinessRules.MinDaysForCustomerChange (2 dias) so para feedback imediato na UI.
// A regra de verdade e sempre validada no servidor.
export const MIN_DAYS_FOR_CUSTOMER_CHANGE = 2;

export function daysUntil(dateIsoString) {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const target = new Date(dateIsoString);
  target.setHours(0, 0, 0, 0);
  return Math.round((target - today) / (1000 * 60 * 60 * 24));
}

export function canSelfServiceChange(appointment) {
  if (appointment.statusCode === 'CANCELLED' || appointment.statusCode === 'COMPLETED') {
    return false;
  }
  return daysUntil(appointment.appointmentDate) >= MIN_DAYS_FOR_CUSTOMER_CHANGE;
}

export function formatDate(dateIsoString) {
  const d = new Date(dateIsoString);
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

export function formatTime(timeString) {
  return (timeString || '').slice(0, 5);
}

export function formatCurrency(value) {
  return Number(value || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
