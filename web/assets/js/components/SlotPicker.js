// Widget reutilizavel: grade de horarios disponiveis (30 em 30 min), usado em
// agendar.html, meus-agendamentos.html (reagendar) e staff/agendamentos.html (reagendar).
export function renderSlotPicker(container, { isClosed, opensAt, closesAt, availableSlots, selected, onSelect }) {
  if (isClosed) {
    container.innerHTML = '<p class="alert alert-warning">O salao nao funciona nesse dia.</p>';
    return;
  }

  if (!availableSlots || availableSlots.length === 0) {
    const opens = (opensAt || '').slice(0, 5);
    const closes = (closesAt || '').slice(0, 5);
    container.innerHTML = `<p class="alert alert-warning">Nenhum horario disponivel neste dia (expediente ${opens}-${closes}). Tente outra data.</p>`;
    return;
  }

  container.innerHTML = `
    <div class="slot-grid">
      ${availableSlots
        .map(
          (slot) => `
            <button type="button" class="btn-secondary btn-small slot-btn${slot === selected ? ' slot-selected' : ''}" data-slot="${slot}">
              ${slot}
            </button>
          `
        )
        .join('')}
    </div>
  `;

  container.querySelectorAll('.slot-btn').forEach((btn) => {
    btn.addEventListener('click', () => onSelect(btn.dataset.slot));
  });
}
