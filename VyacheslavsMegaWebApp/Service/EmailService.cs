using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit.Utils;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.EmailService;

namespace VyacheslavsMegaWebApp.Service
{
    public class EmailService
    {
        private readonly AppResourcesManager _appResources;

        public EmailService(AppResourcesManager appResources) 
        {
            _appResources = appResources;
        }

        public async Task<MimeMessage> CreatePasswordResetMessage(VmsIdentityUser user, string url)
        {
            MimeMessage emailMessage = new MimeMessage();
            BodyBuilder bodyBuilder = new BodyBuilder();

            MimeEntity logoImage = bodyBuilder.LinkedResources.Add(_appResources.GetImage(AppResourcesManager.EmailLogo));
            logoImage.ContentId = MimeUtils.GenerateMessageId();


            bodyBuilder.HtmlBody = setEmailParams(
                await _appResources.GetEmailTemplate(AppResourcesManager.PasswordReset, AppResourcesManager.EmailMessageFormat.Html),
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["resetPasswordUrl"] = url,
                    ["logoCid"] = logoImage.ContentId
                });

            bodyBuilder.TextBody = setEmailParams(
                await _appResources.GetEmailTemplate(AppResourcesManager.PasswordReset, AppResourcesManager.EmailMessageFormat.Text),
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["resetPasswordUrl"] = url
                });

            emailMessage.From.Add(new MailboxAddress(EmailServiceConfig.SenderName, EmailServiceConfig.Email));
            emailMessage.To.Add(new MailboxAddress("", user.Email));
            emailMessage.Subject = "Сброс пароля";
            emailMessage.Body = bodyBuilder.ToMessageBody();

            return emailMessage;
        }

        public async Task<MimeMessage> CreateConfirmMessage(VmsIdentityUser user, string url)
        {
            MimeMessage emailMessage = new MimeMessage();
            BodyBuilder bodyBuilder = new BodyBuilder();

            MimeEntity logoImage = bodyBuilder.LinkedResources.Add(_appResources.GetImage(AppResourcesManager.EmailLogo));
            logoImage.ContentId = MimeUtils.GenerateMessageId();


            bodyBuilder.HtmlBody = setEmailParams(
                await _appResources.GetEmailTemplate(AppResourcesManager.EmailConfirmation, AppResourcesManager.EmailMessageFormat.Html),
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["confirmEmailUrl"] = url,
                    ["logoCid"] = logoImage.ContentId
                });

            bodyBuilder.TextBody = setEmailParams(
                await _appResources.GetEmailTemplate(AppResourcesManager.EmailConfirmation, AppResourcesManager.EmailMessageFormat.Text),
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["confirmEmailUrl"] = url
                });

            emailMessage.From.Add(new MailboxAddress(EmailServiceConfig.SenderName, EmailServiceConfig.Email));
            emailMessage.To.Add(new MailboxAddress("", user.Email));
            emailMessage.Subject = "Подтверждение email";
            emailMessage.Body = bodyBuilder.ToMessageBody();

            return emailMessage;
        }
        
        public async Task<bool> SendEmail(MimeMessage message)
        {
            try
            {
                using (var client = new SmtpClient())
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

        private string setEmailParams(string emailTemplate, Dictionary<string, string> values) 
        {
            foreach (var (key, value) in values)
            {
                emailTemplate = emailTemplate.Replace(
                    $"{{{key}}}",
                    value
                );
            }
            return emailTemplate;
        }
    }
}
