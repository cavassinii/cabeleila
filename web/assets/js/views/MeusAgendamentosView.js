import { formatDate, formatTime, formatCurrency } from '../models/Appointment.js';
import { badgeClassFor } from '../models/AppointmentStatus.js';

export function renderList(container, appointments, onSelect) {
  if (!appointments.length) {
    container.innerHTML = '<div class="empty-state">Nenhum agendamento neste periodo.</div>';
    return;
  }

  container.innerHTML = appointments
    .map(
      (appt) => `
        <div class="list-item" data-id="${appt.id}">
          <div class="list-item-main">
            <div class="list-item-title">${formatDate(appt.appointmentDate)} as ${formatTime(appt.appointmentTime)}</div>
            <div class="list-item-meta">${appt.servicesSummary || 'Sem servicos'} - ${formatCurrency(appt.totalPrice)}</div>
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

// Grade de checkboxes de servicos, com os do agendamento atual ja marcados. Reaproveitado
// tanto para a tela do cliente quanto para a da Leila (edicao de servicos por telefone).
export function renderServiceCheckboxes(container, allServices, selectedIds) {
  const selected = new Set(selectedIds);
  container.innerHTML = allServices
    .map(
      (service) => `
        <label class="checkbox-item">
          <input type="checkbox" name="edit-service" value="${service.id}" ${selected.has(service.id) ? 'checked' : ''} />
          <span>${service.name} <span class="text-muted">(${service.duration_minutes} min)</span></span>
          <span class="service-price">${formatCurrency(service.price)}</span>
        </label>
      `
    )
    .join('');
}

export function renderDetail(container, detail, { canChange, onDateChange, onConfirmReschedule, onToggleEditServices, onSubmitEditServices, onCancel }) {
  container.classList.remove('hidden');

  const itemsHtml = detail.items
    .map(
      (item) => `
        <div class="list-item">
          <div class="list-item-main">
            <div class="list-item-title">${item.serviceName}</div>
            <div class="list-item-meta">${item.durationMinutes} min - ${formatCurrency(item.price)}</div>
          </div>
          <span class="${badgeClassFor(item.statusCode)}">${item.statusDescription}</span>
        </div>
      `
    )
    .join('');

  const actionsHtml = canChange
    ? `
      <div class="btn-row">
        <button type="button" class="btn-secondary btn-small" id="btn-toggle-reschedule">Reagendar</button>
        <button type="button" class="btn-secondary btn-small" id="btn-toggle-edit-services">Editar servicos</button>
        <button type="button" class="btn-danger btn-small" id="btn-cancel">Cancelar agendamento</button>
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
    : `<p class="alert alert-warning">Faltam menos de 2 dias para este agendamento (ou ele ja foi concluido/cancelado). Alteracoes agora so por telefone com o salao.</p>`;

  container.innerHTML = `
    <h2 style="margin-top:0;">Detalhe do agendamento</h2>
    <p><strong>${formatDate(detail.appointmentDate)} as ${formatTime(detail.appointmentTime)}</strong>
      <span class="${badgeClassFor(detail.statusCode)}">${detail.statusDescription}</span>
    </p>
    ${detail.notes ? `<p class="text-muted">Obs: ${detail.notes}</p>` : ''}
    ${itemsHtml}
    <p class="text-right" style="margin-top:0.75rem;"><strong>Total: ${formatCurrency(detail.totalPrice)}</strong></p>
    ${actionsHtml}
  `;

  if (!canChange) {
    return;
  }

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
    if (window.confirm('Tem certeza que deseja cancelar este agendamento?')) {
      onCancel();
    }
  });
}
