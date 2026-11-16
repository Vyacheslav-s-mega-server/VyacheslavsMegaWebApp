using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Base;
using VyacheslavsMegaWebApp.Data.Repositories.Base;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class MainPageRepository : DbRepository
    {
        public MainPageRepository(AppDbContext db) : base(db) { }

        public async Task<MainPageData> GetMainPageData()
        {
            return await DB.MainPageData.OrderBy(m => m.Id).LastAsync();
        }

        public async Task SaveMainPage(MainPageData model)
        {
            DB.Update(model);
            await DB.SaveChangesAsync();
        }
    }
}
