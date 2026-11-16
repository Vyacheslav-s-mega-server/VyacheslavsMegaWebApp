using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class PartnersInfoRepository : DbRepository
    {
        public PartnersInfoRepository(AppDbContext db) : base(db) { }

        public async Task<List<Partner>> GetAllPartners()
        {
            return await DB.Partners.ToListAsync() ?? new List<Partner>();
        }

        public async Task<List<Partner>> GetAllPartnersRandomly()
        {
            return (await GetAllPartners()).OrderBy(p => new Random().Next(int.MaxValue)).ToList();
        }

        public async Task<Partner> GetPartnerById(int id)
        {
            return await DB.Partners.FirstAsync(p => p.Id == id);
        }

        public async Task SavePartner(Partner partner)
        {
            if (partner.Id == 0)
                DB.Entry(partner).State = EntityState.Added;
            else
                DB.Entry(partner).State = EntityState.Modified;

            await DB.SaveChangesAsync();
        }

        public async Task<List<PartnerLink>> GetPartnerLinks(int partnerId)
        {
            return await DB.PartnerLinks.Where(l => l.PartnerId == partnerId).ToListAsync() ?? new List<PartnerLink>();
        }

        public async Task<PartnerLink> GetLinkById(int linkId)
        {
            return await DB.PartnerLinks.FirstAsync(l => l.Id == linkId);
        }

        public async Task RemovePartnerById(int partnerId)
        {
            Partner partnerToRemove = await DB.Partners.FirstAsync(p => p.Id == partnerId);
            DB.Partners.Remove(partnerToRemove);
            await DB.SaveChangesAsync();
        }

        public async Task SaveLink(PartnerLink link)
        {
            if (link.Id == 0)
                DB.Entry(link).State = EntityState.Added;
            else
                DB.Entry(link).State = EntityState.Modified;

            await DB.SaveChangesAsync();
        }

        public async Task RemoveLinkById(int linkId)
        {
            DB.PartnerLinks.Remove(new PartnerLink() { Id =  linkId });
            await DB.SaveChangesAsync();
        }
    }
}
