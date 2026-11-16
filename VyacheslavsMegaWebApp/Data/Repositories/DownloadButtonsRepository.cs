using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class DownloadButtonsRepository : Base.DbRepository
    {
        public DownloadButtonsRepository(AppDbContext db) : base(db) { }

        public async Task<List<DownloadButton>> GetAllButtons()
        {
            return await DB.DownloadButtons.ToListAsync() ?? new List<DownloadButton>();
        }

        public async Task<List<DownloadButton>> GetButtonsToShow()
        {
            return await DB.DownloadButtons.Where(b => b.ShowButton).ToListAsync() ?? new List<DownloadButton>();
        }

        public async Task<DownloadButton> GetButtonById(int id)
        {
            return await DB.DownloadButtons.FirstAsync(c => c.Id == id);
        }

        public async Task SaveButton(DownloadButton button)
        {
            if (button.Id == 0)
                DB.Entry(button).State = EntityState.Added;
            else
                DB.Entry(button).State = EntityState.Modified;

            await DB.SaveChangesAsync();
        }

        public async Task RemoveButtonById(int id)
        {
            DB.Remove(new DownloadButton() { Id = id });
            await DB.SaveChangesAsync();
        }
    }
}
