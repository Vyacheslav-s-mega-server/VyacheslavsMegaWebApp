using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Entities;
using System.Security.Cryptography;
using System.Text;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class LauncherSessionRepository : Base.RepositoryBase
    {
        public async Task<string> CreateSessionKey(VmsIdentityUser user, string? clientToken = null)
        {
            string sessionKey = GenerateSessionKey();
            DB.LauncherSessions.Add(new LauncherSession 
            { 
                SessionKeyHash = HashSessionKey(sessionKey),
                UserId = user.Id,
                ClientToken = clientToken ?? Guid.NewGuid().ToString("N"),
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

        public async Task<string> RefreshSession(string sessionKey, string? clientToken)
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
            DB.LauncherSessions.Remove(session);
            await DB.SaveChangesAsync();
        }

        public async Task RemoveSessionKeys(VmsIdentityUser user)
        {
            List<LauncherSession> existingSessions = DB.LauncherSessions.Where(s => s.UserId == user.Id).ToList();
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
