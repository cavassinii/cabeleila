using DAL.Cabeleila;
using DTO.Cabeleila.Responses;
using System.Threading.Tasks;

namespace API.Application
{
    // Composicao de dados entre appointments + customers + appointment_items + appointment_status,
    // reaproveitada tanto pelo fluxo do cliente quanto pelo painel operacional da Leila.
    public static class AppointmentAssembler
    {
        public static async Task<AppointmentDetailResponse?> BuildDetail(DTO.Cabeleila.Appointment appointment)
        {
            var customer = await Customers.GetById(appointment.Customer_id);
            var status = await AppointmentStatuses.GetById(appointment.Status_id);
            var items = await AppointmentItems.GetDetailByAppointmentId(appointment.Id);

            if (customer == null || status == null)
            {
                return null;
            }

            return new AppointmentDetailResponse
            {
                Id = appointment.Id,
                CustomerId = appointment.Customer_id,
                CustomerName = customer.Full_name,
                AppointmentDate = appointment.Appointment_date,
                AppointmentTime = appointment.Appointment_time,
                StatusCode = status.Code,
                StatusDescription = status.Description,
                Notes = appointment.Notes,
                ConfirmedAt = appointment.Confirmed_at,
                CancelledAt = appointment.Cancelled_at,
                Items = items,
            };
        }
    }
}
