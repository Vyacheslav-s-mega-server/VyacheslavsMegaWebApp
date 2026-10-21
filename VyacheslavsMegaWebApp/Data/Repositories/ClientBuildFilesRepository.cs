using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class ClientBuildFilesRepository : RepositoryBase
    {
        public async Task<List<ClientBuildFile>> GetFilesByBuildSlugAsync(string buildSlug)
        {
            return await DB.ClientBuildFiles
                .Where(f => f.BuildSlug == buildSlug)
                .ToListAsync();
        }

        public async Task<List<ClientBuildFile>> GetFilesByBuildSlugOrderedAsync(string buildSlug)
        {
            return await DB.ClientBuildFiles
                .Where(f => f.BuildSlug == buildSlug)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayName)
                .ToListAsync();
        }

        public async Task AddFileAsync(ClientBuildFile file)
        {
            DB.ClientBuildFiles.Add(file);
            await DB.SaveChangesAsync();
        }

        public async Task<bool?> ToggleRequiredAsync(int id)
        {
            ClientBuildFile? item = await DB.ClientBuildFiles.FindAsync(id);
            if (item == null) return null;
            item.IsRequired = !item.IsRequired;
            await DB.SaveChangesAsync();
            return item.IsRequired;
        }

        public async Task SaveChangesAsync()
        {
            await DB.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ClientBuildFile> files)
        {
            DB.ClientBuildFiles.AddRange(files);
            await DB.SaveChangesAsync();
        }

        public async Task<ClientBuildFile?> GetFileByIdAsync(int id)
        {
            return await DB.ClientBuildFiles.FindAsync(id);
        }

        public async Task<ClientBuildFile?> GetFileBySlugAndPathAsync(string buildSlug, string relativePath)
        {
            return await DB.ClientBuildFiles
                .FirstOrDefaultAsync(f => f.BuildSlug == buildSlug && f.RelativePath == relativePath);
        }

        public async Task UpdateFileAsync(ClientBuildFile file)
        {
            DB.ClientBuildFiles.Update(file);
            await DB.SaveChangesAsync();
        }

        public async Task DeleteFileAsync(int id)
        {
            ClientBuildFile? file = await DB.ClientBuildFiles.FindAsync(id);
            if (file != null)
            {
                DB.ClientBuildFiles.Remove(file);
                await DB.SaveChangesAsync();
            }
        }
    }
}
