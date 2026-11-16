using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class FundRepository : Base.DbRepository
    {
        public FundRepository(AppDbContext db) : base(db) { }

        public enum TransactionState { Added, Removed }

        public async Task<decimal> GetFundState()
        {
            FundSnapshot? lastSnapshot = DB.FundSnapshots.AsEnumerable().MaxBy(s => s.SnapshotDate);
            
            if (lastSnapshot is null)
                return await DB.FundTransactions.SumAsync(t => t.Amount);

            return lastSnapshot.Balance + await DB.FundTransactions.Where(t => t.Date > lastSnapshot.SnapshotDate).SumAsync(t => t.Amount);
        }

        public async Task<List<FundTransaction>> GetLastTransaction(int count)
        {
            return await DB.FundTransactions.OrderByDescending(t => t.Date).Take(count).ToListAsync();
        }

        public async Task<List<FundTransaction>> GetTransactionsAfterId(int lastId, int count)
        {
            return await DB.FundTransactions
                .OrderByDescending(x => x.Id)
                .AsQueryable()
                .Where(x => x.Id < lastId)
                .Take(count)
                .ToListAsync();
        }

        public FundTransaction? GetMaxByType(TransactionPurpose.TransactionType type)
        {
            return type == TransactionPurpose.TransactionType.Income
                ? DB.FundTransactions
                    .OrderByDescending(t => t.Amount)
                    .FirstOrDefault()

                : DB.FundTransactions
                    .OrderBy(t => t.Amount)
                    .FirstOrDefault();
        }

        public FundTransaction? GetMaxByPurpose(TransactionPurpose purpose)
        {
            return purpose.Type == TransactionPurpose.TransactionType.Income
                ? purpose.Transactions.MaxBy(t => t.Amount)
                : purpose.Transactions.MinBy(t => t.Amount);
        }

        public int GetTotalTransactionsCount()
        {
            return DB.FundTransactions.Count();
        }

        public async Task<List<FundTransaction>> GetTransactionsByDate(DateTime dateStart, DateTime dateEnd)
        {
            return await DB.FundTransactions.Where(t => t.Date >= dateStart && t.Date <= dateEnd.AddDays(1).AddSeconds(-1)).OrderByDescending(t => t.Date).ToListAsync();
        }

        public async Task<List<FundTransaction>> GetTransactionsByDate(DateOnly dateStart, DateOnly dateEnd)
        {
            return await GetTransactionsByDate(dateStart.ToDateTime(new TimeOnly()), dateEnd.ToDateTime(new TimeOnly()));
        }

        public async Task<FundTransaction> GetTransactionById(int id)
        {
            return await DB.FundTransactions.FirstAsync(t => t.Id == id);
        }

        public async Task SaveTransaction(FundTransaction transaction)
        {
            using (var DBTransaction = await DB.Database.BeginTransactionAsync())
            {
                try
                {
                    await setTranactionAmountSign(transaction);
                    if (transaction.Id == 0)
                    {
                        DB.Entry(transaction).State = EntityState.Added;
                        await recalcSnapshotsByTransaction(transaction, TransactionState.Added);
                    }
                    else
                    {
                        FundTransaction oldTransaction = await GetTransactionById(transaction.Id);
                        DB.Entry(oldTransaction).State = EntityState.Detached;
                        DB.Entry(transaction).State = EntityState.Modified;

                        //Recalculate snapshots if tranaction has different date or amount
                        if (transaction.Amount != oldTransaction.Amount || transaction.Date != oldTransaction.Date)
                            await recalcSnapshotsByTransaction(oldTransaction, transaction);
                    }
                        
                    await DB.SaveChangesAsync();
                    await DBTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    DBTransaction.Rollback();
                }
            }
        }

        public async Task RemoveTransactionById(int id)
        {
            FundTransaction transaction = await GetTransactionById(id);
            using (var DBTransaction = DB.Database.BeginTransaction())
            {
                try
                {
                    DB.FundTransactions.Remove(transaction);
                    await DB.SaveChangesAsync();
                    await recalcSnapshotsByTransaction(transaction, TransactionState.Removed);
                    await DBTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    DBTransaction.Rollback();
                }
            }
        }

        //Transaction reasons

        public async Task<List<TransactionPurpose>> GetAllTransactionPurposes()
        {
            return await DB.TransactionPurposes.ToListAsync();
        }

        public async Task<TransactionPurpose> GetTransactionPurposeById(int id)
        {
            return await DB.TransactionPurposes.FirstAsync(r => r.Id == id);
        }

        public async Task<List<SelectListItem>> GetPurposesSelectList()
        {
            return await DB.TransactionPurposes.Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToListAsync();
        }

        public async Task SaveTransactionPurpose(TransactionPurpose purpose)
        {
            using (var DBTransaction = await DB.Database.BeginTransactionAsync())
            {
                try
                {
                    if (purpose.Id == 0)
                    {
                        DB.Entry(purpose).State = EntityState.Added;
                    }
                    else
                    {
                        TransactionPurpose oldPurpose = await GetTransactionPurposeById(purpose.Id);
                        if (oldPurpose.Type == purpose.Type)
                        {
                            DB.Entry(oldPurpose).State = EntityState.Detached;
                            DB.Entry(purpose).State = EntityState.Modified;
                        }
                        else
                        {
                            foreach(FundTransaction transaction in oldPurpose.Transactions)
                            {
                                transaction.Amount = -transaction.Amount;
                                await SaveTransaction(transaction);
                            }
                        }
                    }

                    await DB.SaveChangesAsync();
                    await DBTransaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    DBTransaction.Rollback();
                }
            }
        }

        public async Task<bool> RemoveTransactionPurposeById(int id)
        {
            TransactionPurpose purpose = await GetTransactionPurposeById(id);
            try
            {
                DB.TransactionPurposes.Remove(purpose);
                await DB.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> IsPurposeEmpty(int purposeId)
        {
            return !await DB.FundTransactions.AnyAsync(t => t.TransactionPurposeId == purposeId);
        }

        private IQueryable<FundSnapshot> getFundSnapshotsAfterDate(DateTime date)
        {
            return DB.FundSnapshots.Where(s => s.SnapshotDate > date);
        }

        private IQueryable<FundSnapshot> getFundSnapshotsBetweenDates(DateTime dateStart, DateTime dateEnd)
        {
            return DB.FundSnapshots.Where(s => s.SnapshotDate >= dateStart && s.SnapshotDate <= dateEnd);
        }

        private async Task recalcSnapshotsByTransaction(FundTransaction transaction, TransactionState transactionState)
        {
            foreach (FundSnapshot snapshot in getFundSnapshotsAfterDate(transaction.Date))
            {
                snapshot.Balance += transactionState == TransactionState.Added ? transaction.Amount : -transaction.Amount; 
            }
            await DB.SaveChangesAsync();
            return;
        }

        private async Task recalcSnapshotsByTransaction(FundTransaction oldTransaction, FundTransaction newTransaction)
        {
            decimal diffAmount = newTransaction.Amount - oldTransaction.Amount;

            //If new is earlier
            if (newTransaction.Date < oldTransaction.Date)
            {
                foreach (FundSnapshot snapshot in getFundSnapshotsBetweenDates(newTransaction.Date, oldTransaction.Date))
                {
                    snapshot.Balance += newTransaction.Amount;
                }
                foreach (FundSnapshot snapshot in getFundSnapshotsAfterDate(oldTransaction.Date))
                {
                    snapshot.Balance += diffAmount;
                }
                await DB.SaveChangesAsync();
                return;
            }

            //If new is later
            if (newTransaction.Date > oldTransaction.Date)
            {
                foreach (FundSnapshot snapshot in getFundSnapshotsBetweenDates(oldTransaction.Date, newTransaction.Date))
                {
                    snapshot.Balance -= newTransaction.Amount;
                }
                foreach (FundSnapshot snapshot in getFundSnapshotsAfterDate(newTransaction.Date))
                {
                    snapshot.Balance += diffAmount;
                }
                await DB.SaveChangesAsync();
                return;
            }

            //If dates are the same
            foreach (FundSnapshot snapshot in getFundSnapshotsAfterDate(newTransaction.Date))
            {
                snapshot.Balance += diffAmount;
            }
            await DB.SaveChangesAsync();
        }

        private async Task setTranactionAmountSign(FundTransaction transaction)
        {
            TransactionPurpose purpose = await GetTransactionPurposeById(transaction.TransactionPurposeId);
            switch (purpose.Type)
            {
                case TransactionPurpose.TransactionType.Income:
                    transaction.Amount = decimal.Abs(transaction.Amount);
                    return;
                case TransactionPurpose.TransactionType.Expense:
                    transaction.Amount = -decimal.Abs(transaction.Amount);
                    return;
            }
        }
    }
}
