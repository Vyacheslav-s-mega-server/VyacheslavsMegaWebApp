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
        private readonly FriendshipsRepository _friendshipsRepo;
        private readonly LauncherSessionRepository _sessionRepo;
        private readonly UserManager<VmsIdentityUser> _userManager;

        public FriendsApiController(
            FriendshipsRepository friendshipsRepo,
            LauncherSessionRepository sessionRepo,
            UserManager<VmsIdentityUser> userManager)
        {
            _friendshipsRepo = friendshipsRepo;
            _sessionRepo = sessionRepo;
            _userManager = userManager;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetFriendsList([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { error = "Username required." });

            string cleanUser = Path.GetFileNameWithoutExtension(username).Trim();
            VmsIdentityUser? user = await _userManager.FindByNameAsync(cleanUser);
            if (user == null)
                return NotFound(new { error = "User not found." });

            List<Friendship> accepted = await _friendshipsRepo.GetAcceptedFriendsAsync(user.Id);
            List<Friendship> incoming = await _friendshipsRepo.GetIncomingRequestsAsync(user.Id);
            List<Friendship> outgoing = await _friendshipsRepo.GetOutgoingRequestsAsync(user.Id);

            List<string> activeSessionUserIds = await _sessionRepo.GetActiveSessionUserIdsAsync();
            HashSet<string> activeSessionSet = new HashSet<string>(activeSessionUserIds);

            FriendsListResponse response = new FriendsListResponse();

            foreach (Friendship f in accepted)
            {
                bool isRequester = f.RequesterId == user.Id;
                VmsIdentityUser? other = isRequester ? f.Addressee : f.Requester;
                if (other == null) continue;

                string otherName = other.UserName ?? "Player";
                response.Friends.Add(new FriendDto
                {
                    FriendshipId = f.Id,
                    UserId = other.Id,
                    Username = otherName,
                    AvatarUrl = $"/api/skin/{otherName}.png",
                    IsOnline = activeSessionSet.Contains(other.Id),
                    Status = "Accepted",
                    CreatedAt = f.CreatedAt
                });
            }

            foreach (Friendship req in incoming)
            {
                if (req.Requester == null) continue;
                string rName = req.Requester.UserName ?? "Player";
                response.IncomingRequests.Add(new FriendRequestDto
                {
                    FriendshipId = req.Id,
                    RequesterId = req.RequesterId,
                    RequesterUsername = rName,
                    AvatarUrl = $"/api/skin/{rName}.png",
                    CreatedAt = req.CreatedAt
                });
            }

            foreach (Friendship req in outgoing)
            {
                if (req.Addressee == null) continue;
                string aName = req.Addressee.UserName ?? "Player";
                response.OutgoingRequests.Add(new FriendRequestDto
                {
                    FriendshipId = req.Id,
                    RequesterId = req.RequesterId,
                    RequesterUsername = aName,
                    AvatarUrl = $"/api/skin/{aName}.png",
                    CreatedAt = req.CreatedAt
                });
            }

            return Ok(response);
        }

        [HttpPost("request")]
        public async Task<IActionResult> SendFriendRequest([FromBody] SendRequestModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Requester) || string.IsNullOrWhiteSpace(model.Target))
                return BadRequest(new { error = "Requester and Target required." });

            string cleanRequester = Path.GetFileNameWithoutExtension(model.Requester).Trim();
            string cleanTarget = Path.GetFileNameWithoutExtension(model.Target).Trim();

            if (cleanRequester.Equals(cleanTarget, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Cannot add yourself as a friend." });

            VmsIdentityUser? reqUser = await _userManager.FindByNameAsync(cleanRequester);
            VmsIdentityUser? tarUser = await _userManager.FindByNameAsync(cleanTarget);

            if (reqUser == null || tarUser == null)
                return NotFound(new { error = "User not found." });

            Friendship? existing = await _friendshipsRepo.GetFriendshipBetweenAsync(reqUser.Id, tarUser.Id);

            if (existing != null)
            {
                if (existing.Status == FriendshipStatus.Accepted)
                    return BadRequest(new { error = "Already friends." });

                if (existing.Status == FriendshipStatus.Pending)
                {
                    if (existing.RequesterId == tarUser.Id)
                    {
                        existing.Status = FriendshipStatus.Accepted;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _friendshipsRepo.UpdateFriendshipAsync(existing);
                        return Ok(new { success = true, status = "Accepted", message = "Friend request accepted." });
                    }
                    return BadRequest(new { error = "Friend request already sent." });
                }

                existing.Status = FriendshipStatus.Pending;
                existing.RequesterId = reqUser.Id;
                existing.AddresseeId = tarUser.Id;
                existing.CreatedAt = DateTime.UtcNow;
                existing.UpdatedAt = null;
                await _friendshipsRepo.UpdateFriendshipAsync(existing);
                return Ok(new { success = true, status = "Pending", message = "Friend request sent." });
            }

            Friendship friendship = new Friendship
            {
                RequesterId = reqUser.Id,
                AddresseeId = tarUser.Id,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _friendshipsRepo.AddFriendshipAsync(friendship);

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

            Friendship? friendship = await _friendshipsRepo.GetFriendshipByIdAsync(model.FriendshipId);
            if (friendship == null)
                return NotFound(new { error = "Friendship not found." });

            if (friendship.AddresseeId != user.Id)
                return Forbid();

            if (model.Accept)
            {
                friendship.Status = FriendshipStatus.Accepted;
                friendship.UpdatedAt = DateTime.UtcNow;
                await _friendshipsRepo.UpdateFriendshipAsync(friendship);
                return Ok(new { success = true, status = "Accepted" });
            }
            else
            {
                await _friendshipsRepo.RemoveFriendshipAsync(friendship);
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

            Friendship? friendship = await _friendshipsRepo.GetFriendshipBetweenAsync(u1.Id, u2.Id);

            if (friendship == null)
                return NotFound(new { error = "Friendship not found." });

            await _friendshipsRepo.RemoveFriendshipAsync(friendship);

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
                List<Friendship> friendships = await _friendshipsRepo.GetAllFriendshipsForUserAsync(currentUser.Id);

                foreach (Friendship f in friendships)
                {
                    string otherId = f.RequesterId == currentUser.Id ? f.AddresseeId : f.RequesterId;
                    if (f.Status == FriendshipStatus.Accepted)
                        friendIds.Add(otherId);
                    else if (f.Status == FriendshipStatus.Pending)
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
