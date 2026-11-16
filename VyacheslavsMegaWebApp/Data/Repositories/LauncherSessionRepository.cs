using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Entities;
using System.Security.Cryptography;
using System.Text;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class LauncherSessionRepository : Base.DbRepository
    {
        public LauncherSessionRepository(AppDbContext db) : base(db) { }

        public async Task<string> CreateSessionKey(VmsIdentityUser user, string? clientToken = null)
        {
            string sessionKey = GenerateSessionKey();
            DB.LauncherSessions.Add(new LauncherSession 
            { 
                SessionKeyHash = HashSessionKey(sessionKey),
                UserId = user.Id,
                ClientToken = clientToken ?? Guid.NewGuid().ToString("N"),
                //TODO: get session lifetime from config
                ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(30)
            });
            await DB.SaveChangesAsync();
            return sessionKey;
        }

        public async Task<LauncherSession?> GetSessionByKey(string sessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey))
                return null;

            byte[] sessionKeyHash = HashSessionKey(sessionKey);
            return await DB.LauncherSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionKeyHash == sessionKeyHash);
        }

        public async Task<bool> ValidateSession(string sessionKey)
        {
            byte[] sessionKeyHash = HashSessionKey(sessionKey);
            LauncherSession? session = await DB.LauncherSessions.FirstOrDefaultAsync(s => s.SessionKeyHash == sessionKeyHash);
            return !(session is null) && session.ExpiresAt > DateTime.UtcNow;
        }

        public async Task<bool> SetServerId(string sessionKey, string serverId)
        {
            byte[] sessionKeyHash = HashSessionKey(sessionKey);
            LauncherSession? session = await DB.LauncherSessions.FirstOrDefaultAsync(s => s.SessionKeyHash == sessionKeyHash);
            if (session is null) 
                return false;
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

        public async Task<List<string>> GetActiveSessionUserIds()
        {
            return await DB.LauncherSessions
                .Where(s => s.ExpiresAt > DateTime.UtcNow)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();
        }

        /*
        public async Task<LauncherSession?> FindValidSessionAsync(string sessionKey, string userId)
        {
            byte[] keyHash = HashSessionKey(sessionKey);
            LauncherSession? session = await DB.LauncherSessions
                .FirstOrDefaultAsync(s => (s.SessionKeyHash == keyHash) && s.UserId == userId);

            if (session == null || session.ExpiresAt < DateTime.UtcNow)
                return null;

            return session;
        }
        
        
        
        public async Task<List<LauncherSession>> GetSessionsForUserAsync(string userId)
        {
            return await DB.LauncherSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ExpiresAt)
                .ToListAsync();
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
        */

        public async Task<string?> RefreshSession(string sessionKey, string? clientToken)
        {
            byte[] sessionKeyHash = HashSessionKey(sessionKey);
            var session = await DB.LauncherSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionKeyHash == sessionKeyHash);

            if (session is null) 
                return null;

            if (string.IsNullOrEmpty(clientToken) || string.IsNullOrEmpty(session.ClientToken) || session.ClientToken == clientToken)
                return null;

            string newSessionKey = GenerateSessionKey();
            session.SessionKeyHash = HashSessionKey(newSessionKey);
            //TODO: get session lifetime from config
            session.ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(30);
            await DB.SaveChangesAsync();
            return newSessionKey;
        }

        public async Task InvalidateSession(string sessionKey)
        {
            
            byte[] sessionKeyHash = HashSessionKey(sessionKey);
            LauncherSession? session = await DB.LauncherSessions.FirstOrDefaultAsync(s => s.SessionKeyHash == sessionKeyHash);
            if (session is null)
                return;
            await RemoveSessions(session.UserId);
        }

        public async Task<LauncherSession?> GetLastSessionForUser(string userId)
        {
            return await DB.LauncherSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ExpiresAt)
                .FirstOrDefaultAsync();
        }

        public async Task RemoveSessions(string userId)
        {
            List<LauncherSession> existingSessions = DB.LauncherSessions.Where(s => s.UserId == userId).ToList();
            foreach (LauncherSession session in existingSessions)
                DB.Entry(session).State = EntityState.Deleted;
            await DB.SaveChangesAsync();
        }

        #region Private methods

        private byte[] HashSessionKey(string sessionKey)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(sessionKey));
        }

        private string GenerateSessionKey()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        #endregion

    }
}
