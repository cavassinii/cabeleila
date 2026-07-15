import { requireStaff } from '../guard.js';
import { mountNavbar } from './NavController.js';
import { getWeeklyPerformance } from '../services/staffDashboardService.js';
import { ApiError } from '../services/apiClient.js';
import { renderPerformance } from '../views/StaffDashboardView.js';

if (requireStaff()) {
  mountNavbar('dashboard');

  const alertBox = document.getElementById('alert-box');
  const referenceDateInput = document.getElementById('reference-date');
  referenceDateInput.value = new Date().toISOString().slice(0, 10);

  async function load() {
    try {
      const performance = await getWeeklyPerformance(referenceDateInput.value);
      renderPerformance(performance);
    } catch (err) {
      alertBox.textContent = err instanceof ApiError ? err.message : 'Erro ao carregar desempenho.';
      alertBox.classList.remove('hidden');
    }
  }

  document.getElementById('load-btn').addEventListener('click', load);
  load();
}
