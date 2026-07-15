import { loginCustomer, registerCustomer, isAuthenticated, isCustomer } from '../services/authService.js';
import { ApiError } from '../services/apiClient.js';
import { mountNavbar } from './NavController.js';

mountNavbar(null);

if (isAuthenticated() && isCustomer()) {
  window.location.href = 'agendar.html';
}

const alertBox = document.getElementById('alert-box');

function showError(message) {
  alertBox.textContent = message;
  alertBox.classList.remove('hidden');
}

function hideError() {
  alertBox.classList.add('hidden');
}

document.getElementById('login-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  hideError();
  try {
    await loginCustomer({
      email: document.getElementById('login-email').value.trim(),
      password: document.getElementById('login-password').value,
    });
    window.location.href = 'agendar.html';
  } catch (err) {
    showError(err instanceof ApiError ? err.message : 'Nao foi possivel entrar.');
  }
});

document.getElementById('register-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  hideError();
  try {
    await registerCustomer({
      fullName: document.getElementById('register-name').value.trim(),
      email: document.getElementById('register-email').value.trim(),
      phone: document.getElementById('register-phone').value.trim(),
      password: document.getElementById('register-password').value,
    });
    window.location.href = 'agendar.html';
  } catch (err) {
    showError(err instanceof ApiError ? err.message : 'Nao foi possivel criar a conta.');
  }
});
