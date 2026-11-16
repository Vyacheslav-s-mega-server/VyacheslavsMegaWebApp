using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.EmailService;

namespace VyacheslavsMegaWebApp.Service
{
    public class EmailService
    {        
        public async Task<bool> SendEmail(MimeMessage message)
        {
            try
            {
                using (SmtpClient client = new SmtpClient())
                {
                    await client.ConnectAsync(SmtpServerConfig.Address, SmtpServerConfig.Port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(EmailServiceConfig.Email, EmailServiceConfig.Password);
                    await client.SendAsync(message);

                    await client.DisconnectAsync(true);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Не удалось отправить письмо через SMTP ({SmtpServerConfig.Address}:{SmtpServerConfig.Port}): {ex.Message}");
                return false;
            }
        }

        
    }
}
