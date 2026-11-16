using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class GoalRepository : Base.DbRepository
    {
        public GoalRepository(AppDbContext db) : base(db) { }

        public async Task<List<Goal>> GetAllGoals()
        {
            return await DB.Goals.ToListAsync();
        }

        public async Task<Goal?> GetCurrentGoal()
        {
            return await DB.Goals.Where(g => g.IsCurrent).OrderBy(g => g.Date).FirstOrDefaultAsync();
        }

        public async Task<Goal> GetGoalById(int id)
        {
            return await DB.Goals.FirstAsync(g => g.Id == id);
        }

        public async Task SaveGoal(Goal goal)
        {
            if (goal.Id == 0)
                DB.Entry(goal).State = EntityState.Added;
            else
                DB.Entry(goal).State = EntityState.Modified;

            await DB.SaveChangesAsync();
        }

        public async Task RemoveGoalById(int id)
        {
            Goal goal = await GetGoalById(id);
            DB.Goals.Remove(goal);
            await DB.SaveChangesAsync();
        }
    }
}
