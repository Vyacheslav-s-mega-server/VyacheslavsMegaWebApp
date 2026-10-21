using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VyacheslavsMegaWebApp.Api.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<VmsIdentityUser> _userManager;
        private readonly LauncherSessionRepository _sessionRepository;
        private readonly IStringLocalizer<AuthController> _localizer;
        private readonly SkinStorageService _skinStorage;

        public AuthController(
            UserManager<VmsIdentityUser> userManager,
            LauncherSessionRepository sessionRepository,
            IStringLocalizer<AuthController> localizer,
            SkinStorageService skinStorage)
        {
            _userManager = userManager;
            _sessionRepository = sessionRepository;
            _localizer = localizer;
            _skinStorage = skinStorage;
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        [HttpPost("login")]
        [HttpPost(nameof(LoginWithUsername))]
        public async Task<IActionResult> LoginWithUsername([FromBody] LoginWithUsernameRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Unauthorized(new LoginResponse
                    {
                        Succeeded = false,
                        Error = "Missing username or password"
                    });
                }

                string cleanName = request.Username.Trim();
                VmsIdentityUser? user = await _userManager.FindByNameAsync(cleanName);
                if (user is null)
                {
                    return Unauthorized(new LoginResponse 
                    {
                        Succeeded = false,
                        Error = string.Format(_localizer["UserNotFound"], request.Username)
                    });
                }

                if (string.IsNullOrEmpty(user.PasswordHash) || _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Succeeded = false,
                        Error = _localizer["WrongPassword"]
                    });
                }

                string clientToken = string.IsNullOrWhiteSpace(request.ClientToken) ? Guid.NewGuid().ToString("N") : request.ClientToken;
                string sessionKey = await _sessionRepository.CreateSessionKey(user, clientToken);
                string cleanId = user.Id.Replace("-", "");

                string model = _skinStorage.GetSkinModel(user.UserName!);
                string skinUrl = $"{BaseUrl}/api/skin/{Uri.EscapeDataString(user.UserName!)}.png";
                string? capeUrl = _skinStorage.HasCape(user.UserName!) ? $"{BaseUrl}/api/cape/{Uri.EscapeDataString(user.UserName!)}.png" : null;

                ProfileDto profile = new ProfileDto(cleanId, user.UserName!);
                UserDto userDto = new UserDto(cleanId, user.UserName!, user.UserName, model, skinUrl, capeUrl);

                return Ok(new LoginResponse 
                { 
                    Succeeded = true,
                    SessionKey = sessionKey,
                    ClientToken = clientToken,
                    SelectedProfile = profile,
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new LoginResponse
                {
                    Succeeded = false,
                    Error = ex.Message
                });
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateSession([FromBody] ValidateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                return Unauthorized(new { succeeded = false, error = "Invalid token" });
            }

            bool isValid = await _sessionRepository.ValidateSession(request.AccessToken);
            if (!isValid)
            {
                return Unauthorized(new { succeeded = false, error = "Session expired or invalid" });
            }

            LauncherSession? session = await _sessionRepository.GetSessionByToken(request.AccessToken);
            if (session == null || session.User == null)
            {
                return Unauthorized(new { succeeded = false, error = "User not found" });
            }

            string cleanId = session.User.Id.Replace("-", "");
            string model = _skinStorage.GetSkinModel(session.User.UserName!);
            string skinUrl = $"{BaseUrl}/api/skin/{Uri.EscapeDataString(session.User.UserName!)}.png";
            string? capeUrl = _skinStorage.HasCape(session.User.UserName!) ? $"{BaseUrl}/api/cape/{Uri.EscapeDataString(session.User.UserName!)}.png" : null;

            return Ok(new
            {
                succeeded = true,
                user = new UserDto(cleanId, session.User.UserName!, session.User.UserName, model, skinUrl, capeUrl)
            });
        }

        [HttpPost(nameof(LoginWithSessionKey))]
        public async Task<IActionResult> LoginWithSessionKey(LoginWithSessionKeyRequest request)
        {
            bool isValid = await _sessionRepository.ValidateSession(request.SessionKey);
            if (!isValid) return Unauthorized();
            return Ok();
        }
    }
}
