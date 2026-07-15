import { loginStaff, isAuthenticated, isStaff } from '../services/authService.js';
import { ApiError } from '../services/apiClient.js';
import { mountNavbar } from './NavController.js';

mountNavbar(null);

if (isAuthenticated() && isStaff()) {
  window.location.href = 'agendamentos.html';
}

const alertBox = document.getElementById('alert-box');

document.getElementById('staff-login-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  alertBox.classList.add('hidden');
  try {
    await loginStaff({
      email: document.getElementById('login-email').value.trim(),
      password: document.getElementById('login-password').value,
    });
    window.location.href = 'agendamentos.html';
  } catch (err) {
    alertBox.textContent = err instanceof ApiError ? err.message : 'Nao foi possivel entrar.';
    alertBox.classList.remove('hidden');
  }
});
