import { formatDate, formatTime, formatCurrency } from '../models/Appointment.js';
import { badgeClassFor } from '../models/AppointmentStatus.js';

const STATUS_OPTIONS = [
  { value: 'PENDING', label: 'Pendente' },
  { value: 'IN_PROGRESS', label: 'Em andamento' },
  { value: 'COMPLETED', label: 'Concluido' },
  { value: 'CANCELLED', label: 'Cancelado' },
];

export function renderList(container, appointments, onSelect) {
  if (!appointments.length) {
    container.innerHTML = '<div class="empty-state">Nenhum agendamento encontrado.</div>';
    return;
  }

  container.innerHTML = appointments
    .map(
      (appt) => `
        <div class="list-item" data-id="${appt.id}">
          <div class="list-item-main">
            <div class="list-item-title">${appt.customerName} - ${formatDate(appt.appointmentDate)} as ${formatTime(appt.appointmentTime)}</div>
            <div class="list-item-meta">${appt.customerPhone} - ${appt.servicesSummary || 'Sem servicos'} - ${formatCurrency(appt.totalPrice)}</div>
          </div>
          <span class="${badgeClassFor(appt.statusCode)}">${appt.statusDescription}</span>
        </div>
      `
    )
    .join('');

  container.querySelectorAll('.list-item').forEach((el) => {
    el.addEventListener('click', () => onSelect(Number(el.dataset.id)));
  });
}

export function renderDetail(container, detail, { onConfirm, onDateChange, onConfirmReschedule, onToggleEditServices, onSubmitEditServices, onCancel, onUpdateItemStatus }) {
  container.classList.remove('hidden');
  const isOpen = detail.statusCode !== 'CANCELLED' && detail.statusCode !== 'COMPLETED';

  const itemsHtml = detail.items
    .map(
      (item) => `
        <div class="list-item">
          <div class="list-item-main">
            <div class="list-item-title">${item.serviceName}</div>
            <div class="list-item-meta">${item.durationMinutes} min - ${formatCurrency(item.price)}</div>
          </div>
          <span class="${badgeClassFor(item.statusCode)}">${item.statusDescription}</span>
          <select data-item-id="${item.id}" class="item-status-select" style="width:auto;">
            ${STATUS_OPTIONS.map((opt) => `<option value="${opt.value}" ${opt.value === item.statusCode ? 'selected' : ''}>${opt.label}</option>`).join('')}
          </select>
        </div>
      `
    )
    .join('');

  const actionsHtml = isOpen
    ? `
      <div class="btn-row">
        ${detail.statusCode === 'PENDING' ? '<button type="button" class="btn-primary btn-small" id="btn-confirm">Confirmar ao cliente</button>' : ''}
        <button type="button" class="btn-secondary btn-small" id="btn-toggle-reschedule">Reagendar</button>
        <button type="button" class="btn-secondary btn-small" id="btn-toggle-edit-services">Editar servicos</button>
        <button type="button" class="btn-danger btn-small" id="btn-cancel">Cancelar</button>
      </div>

      <div id="reschedule-panel" class="hidden" style="margin-top: 1rem;">
        <div class="form-group">
          <label for="reschedule-date">Nova data</label>
          <input type="date" id="reschedule-date" value="${detail.appointmentDate.slice(0, 10)}" />
        </div>
        <div class="form-group">
          <label>Horario disponivel</label>
          <div id="reschedule-slot-picker"><p class="text-muted">Escolha uma data.</p></div>
        </div>
        <button type="button" class="btn-primary btn-small" id="btn-confirm-reschedule" disabled>Confirmar nova data</button>
      </div>

      <div id="edit-services-panel" class="hidden" style="margin-top: 1rem;">
        <div class="form-group">
          <label>Servicos</label>
          <div id="edit-service-list" class="checkbox-list"></div>
        </div>
        <button type="button" class="btn-primary btn-small" id="btn-save-services">Salvar servicos</button>
      </div>
    `
    : '';

  container.innerHTML = `
    <h2 style="margin-top:0;">${detail.customerName}</h2>
    <p><strong>${formatDate(detail.appointmentDate)} as ${formatTime(detail.appointmentTime)}</strong>
      <span class="${badgeClassFor(detail.statusCode)}">${detail.statusDescription}</span>
    </p>
    ${detail.notes ? `<p class="text-muted">Obs: ${detail.notes}</p>` : ''}
    ${itemsHtml}
    <p class="text-right" style="margin-top:0.75rem;"><strong>Total: ${formatCurrency(detail.totalPrice)}</strong></p>
    ${actionsHtml}
  `;

  container.querySelectorAll('.item-status-select').forEach((select) => {
    select.addEventListener('change', () => {
      onUpdateItemStatus(Number(select.dataset.itemId), select.value);
    });
  });

  if (!isOpen) {
    return;
  }

  const confirmBtn = document.getElementById('btn-confirm');
  if (confirmBtn) confirmBtn.addEventListener('click', onConfirm);

  const reschedulePanel = document.getElementById('reschedule-panel');
  const editServicesPanel = document.getElementById('edit-services-panel');

  document.getElementById('btn-toggle-reschedule').addEventListener('click', () => {
    editServicesPanel.classList.add('hidden');
    reschedulePanel.classList.toggle('hidden');
  });

  document.getElementById('btn-toggle-edit-services').addEventListener('click', () => {
    reschedulePanel.classList.add('hidden');
    const wasHidden = editServicesPanel.classList.contains('hidden');
    editServicesPanel.classList.toggle('hidden');
    if (wasHidden) {
      onToggleEditServices();
    }
  });

  document.getElementById('reschedule-date').addEventListener('change', (event) => onDateChange(event.target.value));
  document.getElementById('btn-confirm-reschedule').addEventListener('click', onConfirmReschedule);
  document.getElementById('btn-save-services').addEventListener('click', () => {
    const ids = Array.from(container.querySelectorAll('input[name="edit-service"]:checked')).map((el) => Number(el.value));
    onSubmitEditServices(ids);
  });

  document.getElementById('btn-cancel').addEventListener('click', () => {
    if (window.confirm('Cancelar este agendamento?')) {
      onCancel();
    }
  });
}
