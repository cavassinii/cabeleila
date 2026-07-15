import { requireStaff } from '../guard.js';
import { mountNavbar } from './NavController.js';
import { listActiveServices, createService, updateService, deactivateService } from '../services/serviceCatalogService.js';
import { ApiError } from '../services/apiClient.js';
import { renderServiceList } from '../views/StaffServicosView.js';

if (requireStaff()) {
  mountNavbar('servicos');

  const alertBox = document.getElementById('alert-box');
  const listEl = document.getElementById('service-list');
  const form = document.getElementById('service-form');
  const idInput = document.getElementById('service-id');
  const nameInput = document.getElementById('service-name');
  const descriptionInput = document.getElementById('service-description');
  const durationInput = document.getElementById('service-duration');
  const priceInput = document.getElementById('service-price');
  const formTitle = document.getElementById('form-title');
  const cancelEditBtn = document.getElementById('cancel-edit-btn');

  function showError(message) {
    alertBox.textContent = message;
    alertBox.classList.remove('hidden');
    alertBox.className = 'alert alert-danger';
  }

  function showSuccess(message) {
    alertBox.textContent = message;
    alertBox.className = 'alert alert-success';
  }

  function resetForm() {
    form.reset();
    idInput.value = '';
    formTitle.textContent = 'Novo servico';
    cancelEditBtn.classList.add('hidden');
  }

  async function loadList() {
    try {
      const services = await listActiveServices();
      renderServiceList(listEl, services, { onEdit: startEdit, onDeactivate: handleDeactivate });
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Erro ao carregar servicos.');
    }
  }

  async function startEdit(id) {
    try {
      const services = await listActiveServices();
      const service = services.find((s) => s.id === id);
      if (!service) return;

      idInput.value = service.id;
      nameInput.value = service.name;
      descriptionInput.value = service.description || '';
      durationInput.value = service.duration_minutes;
      priceInput.value = service.price;
      formTitle.textContent = `Editando: ${service.name}`;
      cancelEditBtn.classList.remove('hidden');
      form.scrollIntoView({ behavior: 'smooth', block: 'start' });
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Erro ao carregar servico.');
    }
  }

  async function handleDeactivate(id) {
    if (!window.confirm('Desativar este servico? Ele deixara de aparecer para novos agendamentos.')) return;
    try {
      await deactivateService(id);
      showSuccess('Servico desativado.');
      await loadList();
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Nao foi possivel desativar.');
    }
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const payload = {
      name: nameInput.value.trim(),
      description: descriptionInput.value.trim() || null,
      durationMinutes: Number(durationInput.value),
      price: Number(priceInput.value),
    };

    try {
      if (idInput.value) {
        await updateService(Number(idInput.value), payload);
        showSuccess('Servico atualizado.');
      } else {
        await createService(payload);
        showSuccess('Servico criado.');
      }
      resetForm();
      await loadList();
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Nao foi possivel salvar o servico.');
    }
  });

  cancelEditBtn.addEventListener('click', resetForm);

  loadList();
}
