using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Entities;
using System.Security.Cryptography;
using System.Text;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class LauncherSessionRepository : Base.RepositoryBase
    {
        public async Task<VmsIdentityUser> GetUserBySessionKey(string sessionKey)
        {
            return (await DB.LauncherSessions.Include(s => s.User).FirstAsync(s => s.SessionKey == sessionKey)).User;
        }

        public async Task<string> CreateSessionKey(VmsIdentityUser user, string? clientToken = null)
        {
            string sessionKey = GenerateSessionKey();
            DB.LauncherSessions.Add(new LauncherSession 
            { 
                SessionKey = sessionKey,
                UserId = user.Id,
                ClientToken = clientToken ?? Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(30)
            });
            await DB.SaveChangesAsync();
            return sessionKey;
        }

        public async Task<LauncherSession?> GetSessionByToken(string token)
        {
            return await DB.LauncherSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionKey == token);
        }

        public async Task<bool> ValidateSession(string token)
        {
            var session = await DB.LauncherSessions.FirstOrDefaultAsync(s => s.SessionKey == token);
            if (session == null) return false;
            return session.ExpiresAt > DateTime.UtcNow;
        }

        public async Task<bool> SetServerId(string token, string serverId)
        {
            var session = await DB.LauncherSessions.FirstOrDefaultAsync(s => s.SessionKey == token);
            if (session == null) return false;
            session.ServerId = serverId;
            await DB.SaveChangesAsync();
            return true;
        }

        public async Task<VmsIdentityUser?> GetUserByServerId(string username, string serverId)
        {
            var session = await DB.LauncherSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ServerId == serverId && s.User.UserName != null && s.User.UserName.ToLower() == username.ToLower());

            return session?.User;
        }

        public async Task<LauncherSession?> FindValidSessionAsync(string sessionKey, string userId)
        {
            byte[] keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(sessionKey));
            LauncherSession? session = await DB.LauncherSessions
                .FirstOrDefaultAsync(s => (s.SessionKeyHash == keyHash || s.SessionKey == sessionKey) && s.UserId == userId);

            if (session == null || session.ExpiresAt < DateTime.UtcNow)
                return null;

            return session;
        }

        public async Task<List<string>> GetActiveSessionUserIdsAsync()
        {
            return await DB.LauncherSessions
                .Where(s => s.ExpiresAt > DateTime.UtcNow)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<LauncherSession>> GetSessionsForUserAsync(string userId)
        {
            return await DB.LauncherSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ExpiresAt)
                .ToListAsync();
        }

        public async Task<LauncherSession?> GetLastSessionForUserAsync(string userId)
        {
            return await DB.LauncherSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ExpiresAt)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteSessionsForUserAsync(string userId)
        {
            List<LauncherSession> sessions = await DB.LauncherSessions
                .Where(s => s.UserId == userId)
                .ToListAsync();
            if (sessions.Count > 0)
            {
                DB.LauncherSessions.RemoveRange(sessions);
                await DB.SaveChangesAsync();
            }
        }

        public async Task<LauncherSession?> RefreshSession(string token, string? clientToken)
        {
            var session = await DB.LauncherSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionKey == token);

            if (session == null) return null;

            if (!string.IsNullOrEmpty(clientToken) && !string.IsNullOrEmpty(session.ClientToken) && session.ClientToken != clientToken)
            {
                return null;
            }

            session.ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(30);
            await DB.SaveChangesAsync();
            return session;
        }

        public async Task InvalidateSession(string token)
        {
            var session = await DB.LauncherSessions.FirstOrDefaultAsync(s => s.SessionKey == token);
            if (session != null)
            {
                DB.LauncherSessions.Remove(session);
                await DB.SaveChangesAsync();
            }
        }

        public async Task RemoveSessionKeys(VmsIdentityUser user)
        {
            List<LauncherSession> existingSessions = DB.LauncherSessions.Where(s => s.UserId == user.Id).ToList();
            foreach (LauncherSession session in existingSessions)
                DB.Entry(session).State = EntityState.Deleted;
            await DB.SaveChangesAsync();
        }

        #region Private methods

        private string GenerateSessionKey()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        private async Task<LauncherSession?> GetSessionByKey(string sessionKey)
        {
            return await DB.LauncherSessions.OrderBy(s => s.ExpiresAt).LastOrDefaultAsync(s => s.SessionKey == sessionKey);
        }

        #endregion

    }
}
