using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class FriendshipsRepository : DbRepository
    {
        public FriendshipsRepository(AppDbContext db) : base(db) { }

        public async Task<List<Friendship>> GetAcceptedFriendsAsync(string userId)
        {
            return await DB.Friendships
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == Friendship.FriendshipStatus.Accepted)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetIncomingRequestsAsync(string userId)
        {
            return await DB.Friendships
                .Include(f => f.Requester)
                .Where(f => f.AddresseeId == userId && f.Status == Friendship.FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetOutgoingRequestsAsync(string userId)
        {
            return await DB.Friendships
                .Include(f => f.Addressee)
                .Where(f => f.RequesterId == userId && f.Status == Friendship.FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<Friendship?> GetFriendshipBetweenAsync(string user1Id, string user2Id)
        {
            return await DB.Friendships
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == user1Id && f.AddresseeId == user2Id) ||
                    (f.RequesterId == user2Id && f.AddresseeId == user1Id));
        }

        public async Task<Friendship?> GetFriendshipById(int friendshipId)
        {
            return await DB.Friendships.FirstOrDefaultAsync(f => f.Id == friendshipId);
        }

        public async Task<List<Friendship>> GetAllFriendshipsForUser(string userId)
        {
            return await DB.Friendships
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                .ToListAsync();
        }

        public async Task<int> GetAcceptedFriendshipsCount(string userId)
        {
            return await DB.Friendships
                .CountAsync(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == Friendship.FriendshipStatus.Accepted);
        }

        public async Task<List<Friendship>> GetFriendshipsBetweenUserAndTargets(string userId, IEnumerable<string> targetUserIds)
        {
            List<string> targetList = targetUserIds.ToList();
            return await DB.Friendships
                .Where(f => (f.RequesterId == userId && targetList.Contains(f.AddresseeId)) ||
                            (f.AddresseeId == userId && targetList.Contains(f.RequesterId)))
                .ToListAsync();
        }

        public async Task<List<Friendship>> GetIncomingPendingRequests(string userId)
        {
            return await DB.Friendships
                .Where(f => f.AddresseeId == userId && f.Status == Friendship.FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task AddFriendship(Friendship friendship)
        {
            DB.Friendships.Add(friendship);
            await DB.SaveChangesAsync();
        }

        public async Task UpdateFriendship(Friendship friendship)
        {
            DB.Friendships.Update(friendship);
            await DB.SaveChangesAsync();
        }

        public async Task RemoveFriendship(Friendship friendship)
        {
            DB.Friendships.Remove(friendship);
            await DB.SaveChangesAsync();
        }
    }
}
