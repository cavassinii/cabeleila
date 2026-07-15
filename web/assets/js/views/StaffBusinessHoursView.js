export function renderHoursForm(container, days) {
  container.innerHTML = days
    .map(
      (day) => `
        <div class="hours-row" data-day="${day.dayOfWeek}">
          <label class="day-name">${day.dayName}</label>
          <input type="time" class="opens-at" step="1800" value="${day.opensAt.slice(0, 5)}" ${day.isClosed ? 'disabled' : ''} />
          <input type="time" class="closes-at" step="1800" value="${day.closesAt.slice(0, 5)}" ${day.isClosed ? 'disabled' : ''} />
          <label class="checkbox-inline">
            <input type="checkbox" class="is-closed" ${day.isClosed ? 'checked' : ''} />
            Fechado
          </label>
        </div>
      `
    )
    .join('');

  container.querySelectorAll('.is-closed').forEach((checkbox) => {
    checkbox.addEventListener('change', () => {
      const row = checkbox.closest('.hours-row');
      const disable = checkbox.checked;
      row.querySelector('.opens-at').disabled = disable;
      row.querySelector('.closes-at').disabled = disable;
    });
  });
}

export function readHoursForm(container) {
  return Array.from(container.querySelectorAll('.hours-row')).map((row) => ({
    dayOfWeek: Number(row.dataset.day),
    opensAt: `${row.querySelector('.opens-at').value}:00`,
    closesAt: `${row.querySelector('.closes-at').value}:00`,
    isClosed: row.querySelector('.is-closed').checked,
  }));
}
