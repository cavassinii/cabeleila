import { requireStaff } from '../guard.js';
import { mountNavbar } from './NavController.js';
import { getBusinessHours, updateBusinessHours } from '../services/businessHoursService.js';
import { ApiError } from '../services/apiClient.js';
import { renderHoursForm, readHoursForm } from '../views/StaffBusinessHoursView.js';

if (requireStaff()) {
  mountNavbar('horarios');

  const alertBox = document.getElementById('alert-box');
  const hoursList = document.getElementById('hours-list');
  const form = document.getElementById('hours-form');

  function showMessage(message, type) {
    alertBox.textContent = message;
    alertBox.className = `alert alert-${type}`;
  }

  getBusinessHours()
    .then((days) => renderHoursForm(hoursList, days))
    .catch((err) => showMessage(err instanceof ApiError ? err.message : 'Erro ao carregar horarios.', 'danger'));

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    try {
      const days = await updateBusinessHours(readHoursForm(hoursList));
      renderHoursForm(hoursList, days);
      showMessage('Horarios salvos com sucesso.', 'success');
    } catch (err) {
      showMessage(err instanceof ApiError ? err.message : 'Nao foi possivel salvar os horarios.', 'danger');
    }
  });
}
