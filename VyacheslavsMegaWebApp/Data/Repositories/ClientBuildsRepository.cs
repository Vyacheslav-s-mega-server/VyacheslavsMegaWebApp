using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class ClientBuildsRepository : Base.RepositoryBase
    {
        public async Task<List<ClientBuildFile>> GetBuildFilesBySlug(string cleanSlug)
        {
            return await DB.ClientBuildFiles.Where(f => f.BuildSlug == cleanSlug).ToListAsync();
        }

        public async Task AddBuildFiles(List<ClientBuildFile> buildFiles)
        {
            DB.ClientBuildFiles.AddRange(buildFiles);
            await DB.SaveChangesAsync();
        }
    }
}
