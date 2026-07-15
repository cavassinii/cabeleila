import { apiClient } from './apiClient.js';
import {
  saveCustomerSession,
  saveStaffSession,
  getCustomerUser,
  getStaffUser,
  getActiveUser,
  getActiveToken,
  clearActiveSession,
} from './storage.js';

function toSessionUser(result) {
  return { id: result.id, fullName: result.fullName, email: result.email, role: result.role };
}

export async function registerCustomer({ fullName, email, phone, password }) {
  const result = await apiClient.post('/auth/customer/register', { fullName, email, phone, password }, { auth: false });
  saveCustomerSession(result.token, toSessionUser(result));
  return result;
}

export async function loginCustomer({ email, password }) {
  const result = await apiClient.post('/auth/customer/login', { email, password }, { auth: false });
  saveCustomerSession(result.token, toSessionUser(result));
  return result;
}

export async function loginStaff({ email, password }) {
  const result = await apiClient.post('/auth/staff/login', { email, password }, { auth: false });
  saveStaffSession(result.token, toSessionUser(result));
  return result;
}

// So encerra a sessao relevante para o tipo de pagina atual (cliente ou equipe),
// sem afetar uma sessao da outra ponta que esteja ativa em outra aba/pagina.
export function logout() {
  clearActiveSession();
}

// Sessao "ativa" para a pagina atual - usado pela navbar pra saber o que mostrar.
export function currentUser() {
  return getActiveUser();
}

export function isAuthenticated() {
  return !!getActiveToken();
}

// Independem da pagina atual: respondem "existe uma sessao valida de equipe/cliente?".
export function isStaff() {
  const user = getStaffUser();
  return !!user && (user.role === 'OWNER' || user.role === 'ATTENDANT');
}

export function isCustomer() {
  const user = getCustomerUser();
  return !!user && user.role === 'Customer';
}
