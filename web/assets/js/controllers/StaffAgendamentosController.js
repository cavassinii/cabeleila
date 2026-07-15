import { requireStaff } from '../guard.js';
import { mountNavbar } from './NavController.js';
import {
  listAppointments,
  getById,
  confirm as confirmAppointment,
  reschedule,
  cancel,
  updateItemStatus,
  editServices,
  getAvailability,
} from '../services/staffAppointmentService.js';
import { listActiveServices } from '../services/serviceCatalogService.js';
import { ApiError } from '../services/apiClient.js';
import { renderList, renderDetail } from '../views/StaffAgendamentosView.js';
import { renderServiceCheckboxes } from '../views/MeusAgendamentosView.js';
import { renderSlotPicker } from '../components/SlotPicker.js';

if (requireStaff()) {
  mountNavbar('agendamentos');

  const alertBox = document.getElementById('alert-box');
  const listEl = document.getElementById('appointment-list');
  const detailEl = document.getElementById('detail-card');
  const dateInput = document.getElementById('filter-date');
  const statusSelect = document.getElementById('filter-status');

  let allServices = [];
  let currentDetail = null;
  let selectedRescheduleTime = null;

  listActiveServices().then((services) => { allServices = services; }).catch(() => {});

  function showError(message) {
    alertBox.textContent = message;
    alertBox.classList.remove('hidden');
  }

  async function loadList() {
    try {
      const appointments = await listAppointments({ date: dateInput.value || undefined, status: statusSelect.value || undefined });
      renderList(listEl, appointments, loadDetail);
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Erro ao carregar agendamentos.');
    }
  }

  async function loadDetail(id) {
    try {
      currentDetail = await getById(id);
      selectedRescheduleTime = null;
      renderDetail(detailEl, currentDetail, {
        onConfirm: () => handleConfirm(id),
        onDateChange: handleRescheduleDateChange,
        onConfirmReschedule: handleConfirmReschedule,
        onToggleEditServices: handleToggleEditServices,
        onSubmitEditServices: handleSubmitEditServices,
        onCancel: () => handleCancel(id),
        onUpdateItemStatus: (itemId, statusCode) => handleUpdateItemStatus(itemId, statusCode),
      });
      detailEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Erro ao carregar detalhe.');
    }
  }

  function totalDuration() {
    return currentDetail.items.reduce((sum, item) => sum + item.durationMinutes, 0);
  }

  async function handleConfirm(id) {
    try {
      await confirmAppointment(id);
      await loadList();
      await loadDetail(id);
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Nao foi possivel confirmar.');
    }
  }

  async function handleRescheduleDateChange(dateValue) {
    const picker = document.getElementById('reschedule-slot-picker');
    const confirmBtn = document.getElementById('btn-confirm-reschedule');
    selectedRescheduleTime = null;
    confirmBtn.disabled = true;

    if (!dateValue) {
      picker.innerHTML = '<p class="text-muted">Escolha uma data.</p>';
      return;
    }

    picker.innerHTML = '<p class="text-muted">Carregando horarios...</p>';
    try {
      const availability = await getAvailability(dateValue, totalDuration(), currentDetail.id);
      renderRescheduleSlots(picker, confirmBtn, availability);
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Erro ao consultar horarios.');
    }
  }

  function renderRescheduleSlots(picker, confirmBtn, availability) {
    renderSlotPicker(picker, {
      isClosed: availability.isClosed,
      opensAt: availability.opensAt,
      closesAt: availability.closesAt,
      availableSlots: availability.availableSlots,
      selected: selectedRescheduleTime,
      onSelect: (slot) => {
        selectedRescheduleTime = slot;
        confirmBtn.disabled = false;
        renderRescheduleSlots(picker, confirmBtn, availability);
      },
    });
  }

  async function handleConfirmReschedule() {
    const dateValue = document.getElementById('reschedule-date').value;
    if (!dateValue || !selectedRescheduleTime) {
      return;
    }

    try {
      await reschedule(currentDetail.id, { appointmentDate: dateValue, appointmentTime: `${selectedRescheduleTime}:00` });
      await loadList();
      await loadDetail(currentDetail.id);
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Nao foi possivel reagendar.');
    }
  }

  function handleToggleEditServices() {
    renderServiceCheckboxes(
      document.getElementById('edit-service-list'),
      allServices,
      currentDetail.items.map((item) => item.serviceId)
    );
  }

  async function handleSubmitEditServices(serviceIds) {
    if (serviceIds.length === 0) {
      showError('Selecione ao menos um servico.');
      return;
    }

    try {
      await editServices(currentDetail.id, serviceIds);
      await loadList();
      await loadDetail(currentDetail.id);
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Nao foi possivel alterar os servicos.');
    }
  }

  async function handleCancel(id) {
    try {
      await cancel(id);
      await loadList();
      await loadDetail(id);
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Nao foi possivel cancelar.');
    }
  }

  async function handleUpdateItemStatus(itemId, statusCode) {
    try {
      await updateItemStatus(itemId, statusCode);
      await loadList();
      if (currentDetail) await loadDetail(currentDetail.id);
    } catch (err) {
      showError(err instanceof ApiError ? err.message : 'Nao foi possivel atualizar o status do servico.');
    }
  }

  document.getElementById('filter-btn').addEventListener('click', loadList);
  document.getElementById('clear-filter-btn').addEventListener('click', () => {
    dateInput.value = '';
    statusSelect.value = '';
    loadList();
  });

  loadList();
}
