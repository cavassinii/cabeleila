import { requireCustomer } from '../guard.js';
import { mountNavbar } from './NavController.js';
import { listActiveServices } from '../services/serviceCatalogService.js';
import { createAppointment, getAvailability } from '../services/appointmentService.js';
import { ApiError } from '../services/apiClient.js';
import { formatDate } from '../models/Appointment.js';
import { renderServiceList, showAlert, hideAlert, showSuggestionModal } from '../views/AgendarView.js';
import { renderSlotPicker } from '../components/SlotPicker.js';

if (requireCustomer()) {
  mountNavbar('agendar');

  const alertBox = document.getElementById('alert-box');
  const suggestionBanner = document.getElementById('suggestion-banner');
  const serviceListEl = document.getElementById('service-list');
  const slotPickerEl = document.getElementById('slot-picker');
  const dateInput = document.getElementById('appointment-date');
  const notesInput = document.getElementById('appointment-notes');
  const form = document.getElementById('appointment-form');
  const submitBtn = document.getElementById('submit-btn');
  const modalRoot = document.getElementById('modal-root');

  let allServices = [];
  let selectedTime = null;

  const today = new Date();
  dateInput.min = today.toISOString().slice(0, 10);

  listActiveServices()
    .then((services) => {
      allServices = services;
      renderServiceList(serviceListEl, services);
      serviceListEl.querySelectorAll('input[name="service"]').forEach((el) => {
        el.addEventListener('change', refreshSlots);
      });
    })
    .catch((err) => showAlert(alertBox, err instanceof ApiError ? err.message : 'Erro ao carregar servicos.'));

  dateInput.addEventListener('change', refreshSlots);

  function getSelectedServiceIds() {
    return Array.from(form.querySelectorAll('input[name="service"]:checked')).map((el) => Number(el.value));
  }

  function getTotalDuration() {
    const ids = new Set(getSelectedServiceIds());
    return allServices.filter((s) => ids.has(s.id)).reduce((sum, s) => sum + s.duration_minutes, 0);
  }

  function updateSubmitState() {
    submitBtn.disabled = !(getSelectedServiceIds().length > 0 && dateInput.value && selectedTime);
  }

  async function refreshSlots() {
    selectedTime = null;
    updateSubmitState();

    const duration = getTotalDuration();
    if (duration === 0 || !dateInput.value) {
      slotPickerEl.innerHTML = '<p class="text-muted">Escolha os servicos e a data para ver os horarios livres.</p>';
      return;
    }

    slotPickerEl.innerHTML = '<p class="text-muted">Carregando horarios...</p>';
    try {
      const availability = await getAvailability(dateInput.value, duration);
      renderSlots(availability);
    } catch (err) {
      showAlert(alertBox, err instanceof ApiError ? err.message : 'Erro ao consultar horarios disponiveis.');
    }
  }

  function renderSlots(availability) {
    renderSlotPicker(slotPickerEl, {
      isClosed: availability.isClosed,
      opensAt: availability.opensAt,
      closesAt: availability.closesAt,
      availableSlots: availability.availableSlots,
      selected: selectedTime,
      onSelect: (slot) => {
        selectedTime = slot;
        updateSubmitState();
        renderSlots(availability);
      },
    });
  }

  async function submitAppointment(keepOriginalDate) {
    const serviceIds = getSelectedServiceIds();
    const payload = {
      appointmentDate: dateInput.value,
      appointmentTime: `${selectedTime}:00`,
      serviceIds,
      notes: notesInput.value.trim() || null,
      keepOriginalDate,
    };

    submitBtn.disabled = true;
    try {
      await createAppointment(payload);
      window.location.href = 'meus-agendamentos.html';
    } catch (err) {
      if (err instanceof ApiError && err.status === 409 && err.data && err.data.hasSuggestion) {
        showSuggestionModal(modalRoot, {
          suggestedDate: formatDate(err.data.suggestedDate),
          onUseSuggested: () => {
            dateInput.value = err.data.suggestedDate.slice(0, 10);
            refreshSlots();
            showAlert(alertBox, 'Escolha um horario disponivel na nova data.', 'info');
          },
          onKeepOriginal: () => submitAppointment(true),
        });
      } else {
        showAlert(alertBox, err instanceof ApiError ? err.message : 'Nao foi possivel agendar.');
      }
    } finally {
      updateSubmitState();
    }
  }

  form.addEventListener('submit', (event) => {
    event.preventDefault();
    hideAlert(alertBox);
    suggestionBanner.classList.add('hidden');

    if (getSelectedServiceIds().length === 0) {
      showAlert(alertBox, 'Selecione ao menos um servico.');
      return;
    }

    if (!selectedTime) {
      showAlert(alertBox, 'Escolha um horario disponivel.');
      return;
    }

    submitAppointment(false);
  });
}
