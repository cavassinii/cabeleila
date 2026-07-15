// Espelha DTO.Cabeleila.AppointmentStatusCodes da API.
export const STATUS_CODES = {
  PENDING: 'PENDING',
  CONFIRMED: 'CONFIRMED',
  IN_PROGRESS: 'IN_PROGRESS',
  COMPLETED: 'COMPLETED',
  CANCELLED: 'CANCELLED',
};

export function badgeClassFor(statusCode) {
  return `badge badge-${(statusCode || '').toLowerCase()}`;
}
