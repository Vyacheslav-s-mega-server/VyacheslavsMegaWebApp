using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using VyacheslavsMegaWebApp.Api.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.MinecraftServer;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.Security;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Api.Controllers
{
    [ApiController]
    public class YggdrasilApiController : ControllerBase
    {
        private readonly UserManager<VmsIdentityUser> _userManager;
        private readonly LauncherSessionRepository _sessionRepository;
        private readonly YggdrasilService _yggdrasil;
        private readonly SkinStorageService _skinStorage;
        private readonly LauncherTicketService _launchTicketService;

        public YggdrasilApiController(
            UserManager<VmsIdentityUser> userManager,
            LauncherSessionRepository sessionRepository,
            YggdrasilService yggdrasil,
            SkinStorageService skinStorage,
            LauncherTicketService launchTicketService)
        {
            _userManager = userManager;
            _sessionRepository = sessionRepository;
            _yggdrasil = yggdrasil;
            _skinStorage = skinStorage;
            _launchTicketService = launchTicketService;
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        [HttpGet("/api/status")]
        public IActionResult GetStatus()
        {
            return Ok(new { status = "online", timestamp = DateTimeOffset.UtcNow });
        }

        [HttpGet("/api/yggdrasil")]
        public IActionResult GetMeta()
        {
            string[] skinDomains = MinecraftServerData.SkinDomains.ToArray();

            var meta = new
            {
                serverName = "VEC Ecosystem",
                implementationName = "VyacheslavsMegaWebApp",
                implementationVersion = "2.0.0"
            };

            return Ok(new YggdrasilMetaResponse(meta, skinDomains, _yggdrasil.GetPublicKeyPem()));
        }

        [HttpPost("/authserver/authenticate")]
        [HttpPost("/api/yggdrasil/authserver/authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return StatusCode(403, new { error = "ForbiddenOperationException", errorMessage = "Invalid credentials. Invalid username or password." });
            }

            string cleanUser = req.Username.Trim();
            VmsIdentityUser? user = await _userManager.FindByNameAsync(cleanUser);
            if (user is null || _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, req.Password) == PasswordVerificationResult.Failed)
            {
                return StatusCode(403, new { error = "ForbiddenOperationException", errorMessage = "Invalid credentials. Invalid username or password." });
            }

            string clientToken = string.IsNullOrWhiteSpace(req.ClientToken) ? Guid.NewGuid().ToString("N") : req.ClientToken;
            string sessionKey = await _sessionRepository.CreateSessionKey(user, clientToken);
            string cleanId = user.Id.Replace("-", "");

            ProfileDto profile = new ProfileDto(cleanId, user.UserName!);
            string model = _skinStorage.GetSkinModel(user.UserName!);
            string skinUrl = $"{BaseUrl}/api/skin/{Uri.EscapeDataString(user.UserName!)}.png";
            string? capeUrl = _skinStorage.HasCape(user.UserName!) ? $"{BaseUrl}/api/cape/{Uri.EscapeDataString(user.UserName!)}.png" : null;

            UserDto userDto = new UserDto(cleanId, user.UserName!, user.UserName, model, skinUrl, capeUrl);

            return Ok(new AuthResponse(
                sessionKey,
                clientToken,
                profile,
                new List<ProfileDto> { profile },
                userDto,
                skinUrl
            ));
        }

        [HttpPost("/authserver/validate")]
        [HttpPost("/api/yggdrasil/authserver/validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.AccessToken))
            {
                return StatusCode(403, new { error = "ForbiddenOperationException", errorMessage = "Invalid token." });
            }

            bool isValid = await _sessionRepository.ValidateSession(req.AccessToken);
            if (!isValid)
            {
                return StatusCode(403, new { error = "ForbiddenOperationException", errorMessage = "Invalid token." });
            }

            return NoContent();
        }

        [HttpPost("/authserver/refresh")]
        [HttpPost("/api/yggdrasil/authserver/refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            LauncherSession? session = await _sessionRepository.GetSessionByKey(req.AccessToken);
            
            if (session is null || session.User is null)
            {
                return StatusCode(403, new { error = "ForbiddenOperationException", errorMessage = "Invalid token." });
            }

            string sessionKey = await _sessionRepository.RefreshSession(req.AccessToken, req.ClientToken);

            VmsIdentityUser user = session.User;
            string cleanId = user.Id.Replace("-", "");
            ProfileDto profile = new ProfileDto(cleanId, user.UserName!);
            string model = _skinStorage.GetSkinModel(user.UserName!);
            string skinUrl = $"{BaseUrl}/api/skin/{Uri.EscapeDataString(user.UserName!)}.png";
            string? capeUrl = _skinStorage.HasCape(user.UserName!) ? $"{BaseUrl}/api/cape/{Uri.EscapeDataString(user.UserName!)}.png" : null;

            UserDto userDto = new UserDto(cleanId, user.UserName!, user.UserName, model, skinUrl, capeUrl);

            return Ok(new AuthResponse(
                sessionKey,
                session.ClientToken ?? Guid.NewGuid().ToString("N"),
                profile,
                new List<ProfileDto> { profile },
                userDto,
                skinUrl
            ));
        }

        [HttpPost("/authserver/invalidate")]
        [HttpPost("/authserver/signout")]
        [HttpPost("/api/yggdrasil/authserver/invalidate")]
        [HttpPost("/api/yggdrasil/authserver/signout")]
        public async Task<IActionResult> Invalidate([FromBody] InvalidateRequest req)
        {
            if (!string.IsNullOrWhiteSpace(req.AccessToken))
            {
                await _sessionRepository.InvalidateSession(req.AccessToken);
            }
            return NoContent();
        }

        [HttpPost("/sessionserver/session/minecraft/join")]
        [HttpPost("/api/yggdrasil/sessionserver/session/minecraft/join")]
        public async Task<IActionResult> Join([FromBody] JoinServerRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.AccessToken) || string.IsNullOrWhiteSpace(req.ServerId))
            {
                return StatusCode(403, new { error = "ForbiddenOperationException", errorMessage = "Invalid parameters." });
            }

            if (!await _sessionRepository.SetServerId(req.AccessToken, req.ServerId))
            {
                return StatusCode(403, new { error = "ForbiddenOperationException", errorMessage = "Invalid token." });
            }

            return NoContent();
        }

        [HttpGet("/sessionserver/session/minecraft/hasJoined")]
        [HttpGet("/api/yggdrasil/sessionserver/session/minecraft/hasJoined")]
        public async Task<IActionResult> HasJoined([FromQuery] string username, [FromQuery] string serverId)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(serverId))
            {
                return NoContent();
            }

            VmsIdentityUser? user = await _sessionRepository.GetUserByServerId(username, serverId);
            if (user is null)
            {
                user = await _userManager.FindByNameAsync(username);
            }

            if (user is null)
            {
                return NoContent();
            }

            if (SecuritySettings.EnforceLauncherTicket && !_launchTicketService.HasActiveTicketForUser(user.UserName!))
            {
                return NoContent();
            }

            string cleanId = user.Id.Replace("-", "");
            PropertyDto prop = _yggdrasil.BuildTexturesProperty(user.UserName!, cleanId, BaseUrl);

            return Ok(new HasJoinedResponse(cleanId, user.UserName!, new List<PropertyDto> { prop }));
        }

        [HttpGet("/sessionserver/session/minecraft/profile/{id}")]
        [HttpGet("/api/yggdrasil/sessionserver/session/minecraft/profile/{id}")]
        public async Task<IActionResult> GetProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NoContent();
            }

            string cleanId = id.Replace("-", "");
            VmsIdentityUser? user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(id);
            }

            string name = user?.UserName ?? id;
            string userId = user?.Id ?? cleanId;
            PropertyDto prop = _yggdrasil.BuildTexturesProperty(name, userId, BaseUrl);

            return Ok(new HasJoinedResponse(cleanId, name, new List<PropertyDto> { prop }));
        }
    }
}
