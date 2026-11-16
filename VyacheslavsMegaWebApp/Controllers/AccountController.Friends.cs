using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Controllers
{
    public partial class AccountController
    {
        /*
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string nameQuery)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(nameQuery))
                return Json(new { success = true, results = new List<FriendSearchUserResult>() });

            nameQuery = nameQuery.Trim();

            List<string> matchedUserIds = _userManager.Users
                .Where(u => EF.Functions.Like(u.UserName, $"%{nameQuery}%"))
                .Take(30)
                .Select(u => u.Id)
                .ToList();

            var friendships = _db.Friendships
                .Where(f => (f.RequesterId == user.Id && matchedIds.Contains(f.AddresseeId)) ||
                            (f.AddresseeId == user.Id && matchedIds.Contains(f.RequesterId)))
                .ToList();

            List<FriendSearchUserResult> matched = new List<FriendSearchUserResult>();

            foreach (var u in matchedUserIds)
            {
                var (siteName, mcUser, _) = _skinStorage.GetUserProfileData(u.UserName);
                string status = "None";
                int? fId = null;

                if (u.Id == user.Id)
                {
                    status = "Self";
                }
                else
                {
                    var f = friendships.FirstOrDefault(x => x.RequesterId == u.Id || x.AddresseeId == u.Id);
                    if (f != null)
                    {
                        fId = f.Id;
                        if (f.Status == Friendship.FriendshipStatus.Accepted) status = "Friends";
                        else if (f.Status == Friendship.FriendshipStatus.Pending)
                        {
                            status = f.RequesterId == user.Id ? "PendingOutgoing" : "PendingIncoming";
                        }
                    }
                }

                matched.Add(new FriendSearchUserResult
                {
                    UserId = u.Id,
                    Username = string.IsNullOrEmpty(siteName) ? (u.UserName ?? "") : siteName,
                    MinecraftUsername = string.IsNullOrEmpty(mcUser) ? (u.UserName ?? "") : mcUser,
                    SkinUrl = _skinStorage.GetSkinUrl(u.UserName ?? ""),
                    FriendshipStatus = status,
                    FriendshipId = fId
                });
            }

            return Json(new { success = true, results = matched });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string targetUserId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (string.IsNullOrEmpty(targetUserId) || targetUserId == user.Id)
                return BadRequest(new { error = "Недопустимый пользователь." });

            var target = await _userManager.FindByIdAsync(targetUserId);
            if (target is null) return NotFound(new { error = "Пользователь не найден." });

            var existing = _db.Friendships
                .FirstOrDefault(f => (f.RequesterId == user.Id && f.AddresseeId == targetUserId) ||
                                     (f.RequesterId == targetUserId && f.AddresseeId == user.Id));

            if (existing != null)
            {
                if (existing.Status == Friendship.FriendshipStatus.Accepted)
                    return BadRequest(new { error = "Вы уже в друзьях." });

                if (existing.Status == Friendship.FriendshipStatus.Pending)
                {
                    if (existing.RequesterId == targetUserId)
                    {
                        existing.Status = Friendship.FriendshipStatus.Accepted;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        return Ok(new { success = true, status = "Friends", message = "Заявка принята! Теперь вы друзья." });
                    }
                    return BadRequest(new { error = "Заявка уже отправлена." });
                }

                existing.RequesterId = user.Id;
                existing.AddresseeId = targetUserId;
                existing.Status = Friendship.FriendshipStatus.Pending;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new { success = true, status = "PendingOutgoing", message = "Заявка в друзья отправлена!", friendshipId = existing.Id });
            }

            var friendship = new Friendship
            {
                RequesterId = user.Id,
                AddresseeId = targetUserId,
                Status = Friendship.FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Friendships.Add(friendship);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, status = "PendingOutgoing", message = "Заявка в друзья отправлена!", friendshipId = friendship.Id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && x.AddresseeId == user.Id);
            if (f is null) return NotFound(new { error = "Заявка не найдена." });

            f.Status = Friendship.FriendshipStatus.Accepted;
            f.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Заявка в друзья принята!" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RejectFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && x.AddresseeId == user.Id);
            if (f is null) return NotFound(new { error = "Заявка не найдена." });

            _db.Friendships.Remove(f);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Заявка отклонена." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveFriend(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && (x.RequesterId == user.Id || x.AddresseeId == user.Id));
            if (f is null) return NotFound(new { error = "Друг не найден." });

            _db.Friendships.Remove(f);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Пользователь удален из друзей." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && x.RequesterId == user.Id);
            if (f is null) return NotFound(new { error = "Заявка не найдена." });

            _db.Friendships.Remove(f);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Заявка отменена." });
        }
        */
    }
}