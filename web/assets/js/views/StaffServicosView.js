import { formatCurrency } from '../models/Appointment.js';

export function renderServiceList(container, services, { onEdit, onDeactivate }) {
  if (!services.length) {
    container.innerHTML = '<div class="empty-state">Nenhum servico cadastrado.</div>';
    return;
  }

  container.innerHTML = services
    .map(
      (service) => `
        <div class="list-item">
          <div class="list-item-main">
            <div class="list-item-title">${service.name}</div>
            <div class="list-item-meta">${service.duration_minutes} min - ${formatCurrency(service.price)}${service.description ? ' - ' + service.description : ''}</div>
          </div>
          <div class="btn-row" style="margin-top:0;">
            <button type="button" class="btn-secondary btn-small" data-action="edit" data-id="${service.id}">Editar</button>
            <button type="button" class="btn-danger btn-small" data-action="deactivate" data-id="${service.id}">Desativar</button>
          </div>
        </div>
      `
    )
    .join('');

  container.querySelectorAll('[data-action="edit"]').forEach((btn) => {
    btn.addEventListener('click', () => onEdit(Number(btn.dataset.id)));
  });
  container.querySelectorAll('[data-action="deactivate"]').forEach((btn) => {
    btn.addEventListener('click', () => onDeactivate(Number(btn.dataset.id)));
  });
}
