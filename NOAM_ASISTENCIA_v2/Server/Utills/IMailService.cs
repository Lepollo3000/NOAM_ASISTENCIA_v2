using NOAM_ASISTENCIA_v2.Server.Models;
//using NOAM_ASISTENCIA_v2.Shared.Utils.Account;

namespace NOAM_ASISTENCIA_v2.Server.Utills
{
    public interface IMailService
    {
        Task SendEmailAsync(string username, string userEmail, string redirectUrl);
        //Task SendRegisterEmailAsync(RegisterRequest request, string redirectUrl);
        Task ResendConfirmationEmailAsync(ApplicationUser request, string redirectUrl);
    }
}
