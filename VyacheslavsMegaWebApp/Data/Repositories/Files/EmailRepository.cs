using MimeKit.Utils;
using MimeKit;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.EmailService;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Data.Repositories.Files
{
    public class EmailRepository : Base.FileRepository
    {
        public EmailRepository(IWebHostEnvironment env) : base(env) { }

        private const string emailConfirmation = "EmailConfirmation";
        private const string emailChange = "EmailChange";
        private const string passwordReset = "PasswordReset";

        private enum EmailMessageFormat { Html, Text }
        
        private string EmailLogo => Path.Combine(AppResourcesFolder, "Images", "logoEmail.png");

        public async Task<MimeMessage> GetPasswordResetMessage(VmsIdentityUser user, string url)
        {
            MimeMessage emailMessage = new MimeMessage();
            BodyBuilder bodyBuilder = new BodyBuilder();

            MimeEntity logoImage = bodyBuilder.LinkedResources.Add(EmailLogo);
            logoImage.ContentId = MimeUtils.GenerateMessageId();

            bodyBuilder.HtmlBody = await GetParameterizedEmail(
                passwordReset,
                EmailMessageFormat.Html,
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["resetPasswordUrl"] = url,
                    ["logoCid"] = logoImage.ContentId
                });

            bodyBuilder.TextBody = await GetParameterizedEmail(
                passwordReset,
                EmailMessageFormat.Text,
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

        public async Task<MimeMessage> GetEmailConfirmMessage(VmsIdentityUser user, string url)
        {
            MimeMessage emailMessage = new MimeMessage();
            BodyBuilder bodyBuilder = new BodyBuilder();

            MimeEntity logoImage = bodyBuilder.LinkedResources.Add(EmailLogo);
            logoImage.ContentId = MimeUtils.GenerateMessageId();

            bodyBuilder.HtmlBody = await GetParameterizedEmail(
                emailConfirmation,
                EmailMessageFormat.Html,
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["confirmEmailUrl"] = url,
                    ["logoCid"] = logoImage.ContentId
                });

            bodyBuilder.TextBody = await GetParameterizedEmail(
                emailConfirmation,
                EmailMessageFormat.Html,
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

        public async Task<MimeMessage> GetEmailChangeMessage(VmsIdentityUser user, string url)
        {
            MimeMessage emailMessage = new MimeMessage();
            BodyBuilder bodyBuilder = new BodyBuilder();

            MimeEntity logoImage = bodyBuilder.LinkedResources.Add(EmailLogo);
            logoImage.ContentId = MimeUtils.GenerateMessageId();

            bodyBuilder.HtmlBody = await GetParameterizedEmail(
                emailChange,
                EmailMessageFormat.Html,
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["confirmEmailUrl"] = url,
                    ["logoCid"] = logoImage.ContentId
                });

            bodyBuilder.TextBody = await GetParameterizedEmail(
                emailChange,
                EmailMessageFormat.Html,
                new Dictionary<string, string>
                {
                    ["userName"] = user.UserName,
                    ["confirmEmailUrl"] = url
                });

            emailMessage.From.Add(new MailboxAddress(EmailServiceConfig.SenderName, EmailServiceConfig.Email));
            emailMessage.To.Add(new MailboxAddress("", user.Email));
            emailMessage.Subject = "Подтверждение смены email";
            emailMessage.Body = bodyBuilder.ToMessageBody();

            return emailMessage;
        }

        #region Private methods

        private async Task<string> GetEmailTemplate(string templateName, EmailMessageFormat format)
        {
            return await File.ReadAllTextAsync(Path.Combine(
                AppResourcesFolder,
                "EmailTemplates",
                templateName,
                string.Concat("Template", format == EmailMessageFormat.Html ? ".html" : ".txt")));
        }

        private async Task<string> GetParameterizedEmail(string templateName, EmailMessageFormat messageFormat, Dictionary<string, string> values)
        {
            string emailText = await GetEmailTemplate(templateName, messageFormat);
            foreach (var (key, value) in values)
            {
                emailText = emailText.Replace(
                    $"{{{key}}}",
                    value
                );
            }
            return emailText;
        }

        #endregion
    }
}
