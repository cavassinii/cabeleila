import { isAuthenticated, isCustomer, isStaff } from './services/authService.js';

// Caminhos relativos: requireCustomer so e chamado a partir das paginas na raiz (web/)
// e requireStaff so a partir das paginas em web/staff/, entao "login.html" resolve
// corretamente para cada caso sem precisar saber o caminho absoluto.
export function requireCustomer() {
  if (!isAuthenticated() || !isCustomer()) {
    window.location.href = 'login.html';
    return false;
  }
  return true;
}

export function requireStaff() {
  if (!isAuthenticated() || !isStaff()) {
    window.location.href = 'login.html';
    return false;
  }
  return true;
}
