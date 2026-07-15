import { formatCurrency } from '../models/Appointment.js';

export function renderServiceList(container, services) {
  if (!services.length) {
    container.innerHTML = '<p class="text-muted">Nenhum servico cadastrado ainda.</p>';
    return;
  }

  container.innerHTML = services
    .map(
      (service) => `
        <label class="checkbox-item">
          <input type="checkbox" name="service" value="${service.id}" />
          <span>${service.name} <span class="text-muted">(${service.duration_minutes} min)</span></span>
          <span class="service-price">${formatCurrency(service.price)}</span>
        </label>
      `
    )
    .join('');
}

export function showAlert(box, message, type = 'danger') {
  box.textContent = message;
  box.className = `alert alert-${type}`;
  box.classList.remove('hidden');
}

export function hideAlert(box) {
  box.classList.add('hidden');
}

export function showSuggestionModal(root, { suggestedDate, onUseSuggested, onKeepOriginal, onDismiss }) {
  root.innerHTML = `
    <div class="modal-backdrop" id="suggestion-backdrop">
      <div class="modal">
        <h3>Mesma semana, mesmo dia?</h3>
        <p>Voce ja tem um agendamento ativo para <strong>${suggestedDate}</strong> nesta semana. Quer marcar este novo servico na mesma data?</p>
        <div class="btn-row">
          <button type="button" class="btn-primary" id="btn-use-suggested">Usar ${suggestedDate}</button>
          <button type="button" class="btn-secondary" id="btn-keep-original">Manter minha data</button>
        </div>
      </div>
    </div>
  `;

  document.getElementById('btn-use-suggested').addEventListener('click', () => {
    root.innerHTML = '';
    onUseSuggested();
  });
  document.getElementById('btn-keep-original').addEventListener('click', () => {
    root.innerHTML = '';
    onKeepOriginal();
  });
  document.getElementById('suggestion-backdrop').addEventListener('click', (event) => {
    if (event.target.id === 'suggestion-backdrop') {
      root.innerHTML = '';
      onDismiss && onDismiss();
    }
  });
}
