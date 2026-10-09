using System.Threading.Tasks;

namespace WebDeveloper.Services.Interfaces
{
    public interface IAppointmentJobService
    {
        Task SendWarningToDoctor(int appointmentId);
        Task CancelUnconfirmedAppointment(int appointmentId);
    }
}
