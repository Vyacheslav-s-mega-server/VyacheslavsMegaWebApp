using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Api.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Repositories;

namespace VyacheslavsMegaWebApp.Api.Controllers
{
    [ApiController]
    [Route("api/friends")]
    public class FriendsApiController : ControllerBase
    {
        private readonly FriendshipsRepository _friendshipsRepository;
        private readonly LauncherSessionRepository _sessionRepository;
        private readonly UserManager<VmsIdentityUser> _userManager;

        public FriendsApiController(
            FriendshipsRepository friendshipsRepository,
            LauncherSessionRepository sessionRepository,
            UserManager<VmsIdentityUser> userManager)
        {
            _friendshipsRepository = friendshipsRepository;
            _sessionRepository = sessionRepository;
            _userManager = userManager;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetFriendsList([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { error = "Username required." });

            string cleanUser = username.Trim();
            VmsIdentityUser? user = await _userManager.FindByNameAsync(cleanUser);
            if (user == null)
                return NotFound(new { error = "User not found." });

            List<Friendship> accepted = await _friendshipsRepository.GetAcceptedFriendsAsync(user.Id);
            List<Friendship> incoming = await _friendshipsRepository.GetIncomingRequestsAsync(user.Id);
            List<Friendship> outgoing = await _friendshipsRepository.GetOutgoingRequestsAsync(user.Id);

            List<string> activeSessionUserIds = await _sessionRepository.GetActiveSessionUserIds();
            HashSet<string> activeSessionSet = new HashSet<string>(activeSessionUserIds);

            FriendsListResponse response = new FriendsListResponse();

            foreach (Friendship friendship in accepted)
            {
                bool isRequester = friendship.RequesterId == user.Id;
                VmsIdentityUser? other = isRequester ? friendship.Addressee : friendship.Requester;
                if (other == null) continue;

                string otherName = other.UserName ?? "Player";
                response.Friends.Add(new FriendDto
                {
                    FriendshipId = friendship.Id,
                    UserId = other.Id,
                    Username = otherName,
                    AvatarUrl = $"/api/skin/{otherName}.png",
                    IsOnline = activeSessionSet.Contains(other.Id),
                    Status = "Accepted",
                    CreatedAt = friendship.CreatedAt
                });
            }

            foreach (Friendship friendshipRequest in incoming)
            {
                if (friendshipRequest.Requester == null) continue;
                string rName = friendshipRequest.Requester.UserName ?? "Player";
                response.IncomingRequests.Add(new FriendRequestDto
                {
                    FriendshipId = friendshipRequest.Id,
                    RequesterId = friendshipRequest.RequesterId,
                    RequesterUsername = rName,
                    AvatarUrl = $"/api/skin/{rName}.png",
                    CreatedAt = friendshipRequest.CreatedAt
                });
            }

            foreach (Friendship friendshipRequest in outgoing)
            {
                if (friendshipRequest.Addressee == null) continue;
                string aName = friendshipRequest.Addressee.UserName ?? "Player";
                response.OutgoingRequests.Add(new FriendRequestDto
                {
                    FriendshipId = friendshipRequest.Id,
                    RequesterId = friendshipRequest.RequesterId,
                    RequesterUsername = aName,
                    AvatarUrl = $"/api/skin/{aName}.png",
                    CreatedAt = friendshipRequest.CreatedAt
                });
            }

            return Ok(response);
        }

        [HttpPost("request")]
        public async Task<IActionResult> SendFriendRequest([FromBody] SendRequestModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Requester) || string.IsNullOrWhiteSpace(model.Target))
                return BadRequest(new { error = "Requester and Target required." });

            string cleanRequester = model.Requester.Trim();
            string cleanTarget = model.Target.Trim();

            if (cleanRequester.Equals(cleanTarget, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Cannot add yourself as a friend." });

            VmsIdentityUser? requestUser = await _userManager.FindByNameAsync(cleanRequester);
            VmsIdentityUser? tarUser = await _userManager.FindByNameAsync(cleanTarget);

            if (requestUser == null || tarUser == null)
                return NotFound(new { error = "User not found." });

            Friendship? existing = await _friendshipsRepository.GetFriendshipBetweenAsync(requestUser.Id, tarUser.Id);

            if (existing != null)
            {
                if (existing.Status == Friendship.FriendshipStatus.Accepted)
                    return BadRequest(new { error = "Already friends." });

                if (existing.Status == Friendship.FriendshipStatus.Pending)
                {
                    if (existing.RequesterId == tarUser.Id)
                    {
                        existing.Status = Friendship.FriendshipStatus.Accepted;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _friendshipsRepository.UpdateFriendship(existing);
                        return Ok(new { success = true, status = "Accepted", message = "Friend request accepted." });
                    }
                    return BadRequest(new { error = "Friend request already sent." });
                }

                existing.Status = Friendship.FriendshipStatus.Pending;
                existing.RequesterId = requestUser.Id;
                existing.AddresseeId = tarUser.Id;
                existing.CreatedAt = DateTime.UtcNow;
                existing.UpdatedAt = null;
                await _friendshipsRepository.UpdateFriendship(existing);
                return Ok(new { success = true, status = "Pending", message = "Friend request sent." });
            }

            Friendship friendship = new Friendship
            {
                RequesterId = requestUser.Id,
                AddresseeId = tarUser.Id,
                Status = Friendship.FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _friendshipsRepository.AddFriendship(friendship);

            return Ok(new { success = true, status = "Pending", message = "Friend request sent." });
        }

        [HttpPost("respond")]
        public async Task<IActionResult> RespondToFriendRequest([FromBody] RespondModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username))
                return BadRequest(new { error = "Username required." });

            string cleanUser = Path.GetFileNameWithoutExtension(model.Username).Trim();
            VmsIdentityUser? user = await _userManager.FindByNameAsync(cleanUser);
            if (user == null)
                return NotFound(new { error = "User not found." });

            Friendship? friendship = await _friendshipsRepository.GetFriendshipById(model.FriendshipId);
            if (friendship == null)
                return NotFound(new { error = "Friendship not found." });

            if (friendship.AddresseeId != user.Id)
                return Forbid();

            if (model.Accept)
            {
                friendship.Status = Friendship.FriendshipStatus.Accepted;
                friendship.UpdatedAt = DateTime.UtcNow;
                await _friendshipsRepository.UpdateFriendship(friendship);
                return Ok(new { success = true, status = "Accepted" });
            }
            else
            {
                await _friendshipsRepository.RemoveFriendship(friendship);
                return Ok(new { success = true, status = "Declined" });
            }
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFriend([FromBody] RemoveFriendModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Target))
                return BadRequest(new { error = "Username and Target required." });

            string cleanUser = Path.GetFileNameWithoutExtension(model.Username).Trim();
            string cleanTarget = Path.GetFileNameWithoutExtension(model.Target).Trim();

            VmsIdentityUser? u1 = await _userManager.FindByNameAsync(cleanUser);
            VmsIdentityUser? u2 = await _userManager.FindByNameAsync(cleanTarget);

            if (u1 == null || u2 == null)
                return NotFound(new { error = "User not found." });

            Friendship? friendship = await _friendshipsRepository.GetFriendshipBetweenAsync(u1.Id, u2.Id);

            if (friendship == null)
                return NotFound(new { error = "Friendship not found." });

            await _friendshipsRepository.RemoveFriendship(friendship);

            return Ok(new { success = true, message = "Friend removed." });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] string? current)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Ok(new List<SearchUserDto>());

            string query = q.Trim().ToLower();
            VmsIdentityUser? currentUser = !string.IsNullOrWhiteSpace(current) ? await _userManager.FindByNameAsync(current.Trim()) : null;

            List<VmsIdentityUser> matchedUsers = await _userManager.Users
                .Where(u => u.UserName != null && u.UserName.ToLower().Contains(query))
                .Take(15)
                .ToListAsync();

            HashSet<string> friendIds = new HashSet<string>();
            HashSet<string> pendingIds = new HashSet<string>();

            if (currentUser != null)
            {
                List<Friendship> friendships = await _friendshipsRepository.GetAllFriendshipsForUser(currentUser.Id);

                foreach (Friendship f in friendships)
                {
                    string otherId = f.RequesterId == currentUser.Id ? f.AddresseeId : f.RequesterId;
                    if (f.Status == Friendship.FriendshipStatus.Accepted)
                        friendIds.Add(otherId);
                    else if (f.Status == Friendship.FriendshipStatus.Pending)
                        pendingIds.Add(otherId);
                }
            }

            List<SearchUserDto> result = new List<SearchUserDto>();
            foreach (VmsIdentityUser u in matchedUsers)
            {
                if (currentUser != null && u.Id == currentUser.Id) continue;
                string uName = u.UserName ?? "Player";
                result.Add(new SearchUserDto
                {
                    Id = u.Id,
                    Username = uName,
                    AvatarUrl = $"/api/skin/{uName}.png",
                    IsFriend = friendIds.Contains(u.Id),
                    IsPending = pendingIds.Contains(u.Id)
                });
            }

            return Ok(result);
        }
    }
}
