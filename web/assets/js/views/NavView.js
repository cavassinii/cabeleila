// Caminhos relativos ao diretorio da pagina atual: CUSTOMER_LINKS so aparece em
// paginas na raiz (web/) e STAFF_LINKS so em paginas dentro de web/staff/,
// entao nenhum dos dois precisa subir ou descer de diretorio.
const CUSTOMER_LINKS = [
  { href: 'agendar.html', key: 'agendar', label: 'Agendar' },
  { href: 'meus-agendamentos.html', key: 'meus-agendamentos', label: 'Meus agendamentos' },
];

const STAFF_LINKS = [
  { href: 'agendamentos.html', key: 'agendamentos', label: 'Agendamentos' },
  { href: 'servicos.html', key: 'servicos', label: 'Servicos' },
  { href: 'horarios.html', key: 'horarios', label: 'Horarios' },
  { href: 'dashboard.html', key: 'dashboard', label: 'Desempenho' },
];

export function renderNavbar({ role, userName, active }) {
  const links = role === 'staff' ? STAFF_LINKS : role === 'customer' ? CUSTOMER_LINKS : [];
  const brandHref = role === 'staff' ? 'agendamentos.html' : role === 'customer' ? 'agendar.html' : 'login.html';

  const linksHtml = links
    .map(
      (link) =>
        `<a href="${link.href}" class="${link.key === active ? 'active' : ''}" style="${
          link.key === active ? 'color: var(--color-primary); font-weight: 600;' : ''
        }">${link.label}</a>`
    )
    .join('');

  const userHtml = userName
    ? `<span class="user-chip">${userName}</span><a href="#" id="logout-link">Sair</a>`
    : '';

  return `
    <a class="brand" href="${brandHref}">Cabeleila</a>
    <nav>
      ${linksHtml}
      ${userHtml}
    </nav>
  `;
}
