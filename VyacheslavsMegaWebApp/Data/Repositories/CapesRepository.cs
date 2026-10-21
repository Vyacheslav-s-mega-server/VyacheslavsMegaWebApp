using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class CapesRepository : RepositoryBase
    {
        public void SeedDefaultCapesIfNeeded(Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            if (!DB.CustomCapes.Any(c => c.Slug == "vec"))
            {
                DataSeeder.SeedDefaultCapes(DB, env);
            }
        }

        public async Task<List<CustomCape>> GetPublicCapesAsync()
        {
            return await DB.CustomCapes
                .Where(c => c.IsPublic)
                .ToListAsync();
        }

        public async Task<List<UserCapeOwnership>> GetUserCapeOwnershipsAsync(string userId)
        {
            return await DB.UserCapeOwnerships
                .Include(o => o.Cape)
                .Where(o => o.UserId == userId)
                .ToListAsync();
        }

        public async Task<CustomCape?> GetCapeByIdAsync(int id)
        {
            return await DB.CustomCapes.FindAsync(id);
        }

        public async Task<CustomCape?> GetCapeBySlugAsync(string slug)
        {
            return await DB.CustomCapes
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task<bool> IsCapeOwnedByUserAsync(string userId, int capeId)
        {
            return await DB.UserCapeOwnerships
                .AnyAsync(o => o.UserId == userId && o.CapeId == capeId);
        }

        public async Task<CapeRedeemCode?> GetRedeemCodeAsync(string code)
        {
            return await DB.CapeRedeemCodes
                .Include(c => c.Cape)
                .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
        }

        public async Task AddOwnershipAsync(UserCapeOwnership ownership)
        {
            DB.UserCapeOwnerships.Add(ownership);
            await DB.SaveChangesAsync();
        }

        public async Task UpdateRedeemCodeAsync(CapeRedeemCode code)
        {
            DB.CapeRedeemCodes.Update(code);
            await DB.SaveChangesAsync();
        }

        public async Task<bool> AnyCapeWithSlugAsync(string slug)
        {
            return await DB.CustomCapes.AnyAsync(c => c.Slug == slug);
        }

        public async Task<bool> AnyCapeWithSlugExceptIdAsync(string slug, int exceptId)
        {
            return await DB.CustomCapes.AnyAsync(c => c.Slug == slug && c.Id != exceptId);
        }

        public async Task<List<CustomCape>> GetAllCapesWithDetailsAsync()
        {
            return await DB.CustomCapes
                .Include(c => c.Codes)
                .Include(c => c.Ownerships)
                .OrderBy(c => c.Slug == "vec" ? 0 : 1)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<CustomCape?> GetCapeWithCodesByIdAsync(int id)
        {
            return await DB.CustomCapes
                .Include(c => c.Codes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddCapeAsync(CustomCape cape)
        {
            DB.CustomCapes.Add(cape);
            await DB.SaveChangesAsync();
        }

        public async Task UpdateCapeAsync(CustomCape cape)
        {
            DB.CustomCapes.Update(cape);
            await DB.SaveChangesAsync();
        }

        public async Task<bool> DeleteCapeAsync(int id)
        {
            CustomCape? cape = await DB.CustomCapes.FindAsync(id);
            if (cape == null || cape.Slug == "vec")
            {
                return false;
            }
            DB.CustomCapes.Remove(cape);
            await DB.SaveChangesAsync();
            return true;
        }

        public async Task<HashSet<string>> GetExistingCodesHashSetAsync()
        {
            List<string> codes = await DB.CapeRedeemCodes.Select(c => c.Code).ToListAsync();
            return codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task AddCodesRangeAsync(IEnumerable<CapeRedeemCode> codes)
        {
            await DB.CapeRedeemCodes.AddRangeAsync(codes);
            await DB.SaveChangesAsync();
        }

        public async Task<bool> DeleteCodeAsync(int id, int capeId)
        {
            CapeRedeemCode? code = await DB.CapeRedeemCodes.FirstOrDefaultAsync(c => c.Id == id && c.CapeId == capeId);
            if (code == null) return false;
            DB.CapeRedeemCodes.Remove(code);
            await DB.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, string?>> GetUsersMapAsync(IEnumerable<string> userIds)
        {
            return await DB.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);
        }
    }
}
