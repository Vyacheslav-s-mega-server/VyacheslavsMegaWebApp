using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class FriendshipsRepository : RepositoryBase
    {
        public async Task<List<Friendship>> GetAcceptedFriendsAsync(string userId)
        {
            return await DB.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetIncomingRequestsAsync(string userId)
        {
            return await DB.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetOutgoingRequestsAsync(string userId)
        {
            return await DB.Friendships
                .Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<Friendship?> GetFriendshipBetweenAsync(string user1Id, string user2Id)
        {
            return await DB.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == user1Id && f.AddresseeId == user2Id) ||
                    (f.RequesterId == user2Id && f.AddresseeId == user1Id));
        }

        public async Task<Friendship?> GetFriendshipByIdAsync(int friendshipId)
        {
            return await DB.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
        }

        public async Task<List<Friendship>> GetAllFriendshipsForUserAsync(string userId)
        {
            return await DB.Friendships
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                .ToListAsync();
        }

        public async Task<int> GetAcceptedCountAsync(string userId)
        {
            return await DB.Friendships
                .CountAsync(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted);
        }

        public async Task<List<Friendship>> GetFriendshipsBetweenUserAndTargetsAsync(string userId, IEnumerable<string> targetUserIds)
        {
            List<string> targetList = targetUserIds.ToList();
            return await DB.Friendships
                .Where(f => (f.RequesterId == userId && targetList.Contains(f.AddresseeId)) ||
                            (f.AddresseeId == userId && targetList.Contains(f.RequesterId)))
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetIncomingPendingRequestsAsync(string userId)
        {
            return await DB.Friendships
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task AddFriendshipAsync(Friendship friendship)
        {
            DB.Friendships.Add(friendship);
            await DB.SaveChangesAsync();
        }

        public async Task UpdateFriendshipAsync(Friendship friendship)
        {
            DB.Friendships.Update(friendship);
            await DB.SaveChangesAsync();
        }

        public async Task RemoveFriendshipAsync(Friendship friendship)
        {
            DB.Friendships.Remove(friendship);
            await DB.SaveChangesAsync();
        }
    }
}
