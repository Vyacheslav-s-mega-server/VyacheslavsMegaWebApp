using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class ServerMediaLinksRepository : DbRepository
    {
        public ServerMediaLinksRepository(AppDbContext db) : base(db) { }

        public async Task<List<ServerMediaLink>> GetAllLinks()
        {
            return await DB.ServerMediaLinks.ToListAsync();
        }

        public async Task<ServerMediaLink> GetLinkById(int linkId) 
        {
            return await DB.ServerMediaLinks.FirstAsync(l => l.Id == linkId);
        }

        public async Task<ServerMediaLink> GetNoTrackedLinkById(int linkId)
        {
            return await DB.ServerMediaLinks.AsNoTracking().FirstAsync(l => l.Id == linkId);
        }

        public async Task SaveLink(ServerMediaLink link)
        {
            if (link.Id == 0)
                DB.Entry(link).State = EntityState.Added;
            else
                DB.Entry(link).State = EntityState.Modified;

            await DB.SaveChangesAsync();
        }

        public async Task RemoveLinkById(int linkId)
        {
            DB.ServerMediaLinks.Remove(new ServerMediaLink() { Id = linkId });
            await DB.SaveChangesAsync();
        }
    }
}
