namespace VyacheslavsMegaWebApp.Data.Repositories.Base
{
    public abstract class DbRepository
    {
        private readonly AppDbContext _db;

        public DbRepository(AppDbContext db)
        {
            _db = db;
        }   

        protected AppDbContext DB => _db;
    }
}
