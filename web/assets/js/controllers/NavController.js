import { currentUser, logout } from '../services/authService.js';
import { renderNavbar } from '../views/NavView.js';

export function mountNavbar(active) {
  const mount = document.getElementById('navbar');
  if (!mount) return;

  // O papel vem do proprio usuario retornado por currentUser() (ja e a sessao certa
  // para o contexto desta pagina - cliente na raiz, equipe em /staff/). Nao recalcular
  // isso checando isStaff()/isCustomer() aqui, pois esses checks olham a sessao daquele
  // papel especifico em qualquer lugar do localStorage, ignorando a pagina atual - o que
  // fazia o menu da equipe aparecer numa pagina de cliente so por existir uma sessao da
  // Leila salva (de outra aba, por exemplo).
  const user = currentUser();
  const role = user ? (user.role === 'OWNER' || user.role === 'ATTENDANT' ? 'staff' : 'customer') : null;

  mount.innerHTML = renderNavbar({ role, userName: user ? user.fullName : null, active });

  const logoutLink = document.getElementById('logout-link');
  if (logoutLink) {
    logoutLink.addEventListener('click', (event) => {
      event.preventDefault();
      logout();
      // "login.html" resolve certo em ambos os casos: e sempre um irmao no mesmo diretorio
      // da pagina atual (web/login.html ou web/staff/login.html).
      window.location.href = 'login.html';
    });
  }
}
