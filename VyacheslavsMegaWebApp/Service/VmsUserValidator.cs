using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Linq;
using System.Text.RegularExpressions;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration;

namespace VyacheslavsMegaWebApp.Service
{
    public class VmsUserValidator : UserValidator<VmsIdentityUser>
    {
        private readonly VmsIdentityErrorDescriber _errorDescriber;

        public VmsUserValidator(IdentityErrorDescriber errorDescriber)
        {
            _errorDescriber = errorDescriber as VmsIdentityErrorDescriber ?? throw new InvalidOperationException("Зарегистрирован неверный IdentityErrorDescriber.");
        }

        public static bool ValidateEmail(string email)
        {
            Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return ProjectConfig.AcceptedEmailDomains.Contains(GetEmailDomain(email)) && emailRegex.IsMatch(email);
        }

        public override async Task<IdentityResult> ValidateAsync(UserManager<VmsIdentityUser> manager, VmsIdentityUser user)
        {
            List<IdentityError> errors = new List<IdentityError>();

            if (!ValidateEmail(user.Email!))
            {
                errors.Add(_errorDescriber.UnallowedEmailDomain(GetEmailDomain(user.Email!)));
            }

            return await Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
        }

        private static string GetEmailDomain(string email)
        {
            return email.Substring(email.IndexOf('@') + 1).ToLower(); 
        }
    }
}
