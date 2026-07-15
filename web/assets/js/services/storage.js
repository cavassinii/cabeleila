// Cliente e equipe tem sessoes independentes (chaves separadas no localStorage),
// para logar como Leila numa aba nao derrubar a sessao do cliente logado em outra
// (e vice-versa) - as duas paginas rodam na mesma origem e compartilhariam o
// mesmo localStorage se usassem a mesma chave.
const CUSTOMER_TOKEN_KEY = 'cabeleila_customer_token';
const CUSTOMER_USER_KEY = 'cabeleila_customer_user';
const STAFF_TOKEN_KEY = 'cabeleila_staff_token';
const STAFF_USER_KEY = 'cabeleila_staff_user';

// A pagina atual sempre pertence a um dos dois contextos (raiz = cliente, /staff/ = equipe),
// entao da pra descobrir qual sessao usar automaticamente pelo caminho da URL.
function isStaffContext() {
  return location.pathname.includes('/staff/');
}

export function saveCustomerSession(token, user) {
  localStorage.setItem(CUSTOMER_TOKEN_KEY, token);
  localStorage.setItem(CUSTOMER_USER_KEY, JSON.stringify(user));
}

export function saveStaffSession(token, user) {
  localStorage.setItem(STAFF_TOKEN_KEY, token);
  localStorage.setItem(STAFF_USER_KEY, JSON.stringify(user));
}

export function getCustomerToken() {
  return localStorage.getItem(CUSTOMER_TOKEN_KEY);
}

export function getStaffToken() {
  return localStorage.getItem(STAFF_TOKEN_KEY);
}

export function getCustomerUser() {
  const raw = localStorage.getItem(CUSTOMER_USER_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function getStaffUser() {
  const raw = localStorage.getItem(STAFF_USER_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function clearCustomerSession() {
  localStorage.removeItem(CUSTOMER_TOKEN_KEY);
  localStorage.removeItem(CUSTOMER_USER_KEY);
}

export function clearStaffSession() {
  localStorage.removeItem(STAFF_TOKEN_KEY);
  localStorage.removeItem(STAFF_USER_KEY);
}

// Usados pelo apiClient/authService, que nao sabem (nem precisam saber) em qual
// pagina estao rodando - so precisam da sessao relevante para o contexto atual.
export function getActiveToken() {
  return isStaffContext() ? getStaffToken() : getCustomerToken();
}

export function getActiveUser() {
  return isStaffContext() ? getStaffUser() : getCustomerUser();
}

export function clearActiveSession() {
  if (isStaffContext()) {
    clearStaffSession();
  } else {
    clearCustomerSession();
  }
}
