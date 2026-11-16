using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class UserReportsRepository : DbRepository
    {
        public UserReportsRepository(AppDbContext db) : base(db) { }

        public async Task SaveReportAsync(UserReport report)
        {
            DB.UserReports.Add(report);
            await DB.SaveChangesAsync();
        } 

        public async Task<List<UserReport>> GetAllReportsAsync()
        {
            return await DB.UserReports.ToListAsync();
        }

        public async Task<UserReport> GetReportByIdAsync(int id)
        {
            return await DB.UserReports.FirstAsync(r => r.Id == id);
        }

        public async Task RemoveReportsByIdAsync(int id)
        {
            UserReport reportToRemove = await DB.UserReports.FirstAsync(r => r.Id == id);
            DB.UserReports.Remove(reportToRemove);
            await DB.SaveChangesAsync();
        }
    }
}
