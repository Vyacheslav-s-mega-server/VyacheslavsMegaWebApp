using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories.Base;

namespace VyacheslavsMegaWebApp.Data.Repositories
{
    public class ContactsInfoRepository : RepositoryBase
    {
        public async Task<List<Contact>> GetAllContacts()
        {
            return await DB.Contacts.Include(c => c.User).Include(c => c.Links).ToListAsync() ?? new List<Contact>();
        }

        public async Task<Contact> GetContactById(int id)
        {
            return await DB.Contacts.FirstAsync(c => c.Id == id);
        }

        public async Task SaveContact(Contact contact)
        {
            if (contact.Id == 0) 
                DB.Entry(contact).State = EntityState.Added;
            else
                DB.Entry(contact).State = EntityState.Modified;
            
            await DB.SaveChangesAsync();
        }

        public async Task<List<ContactLink>> GetContactLinks(int contactId)
        {
            return await DB.ContactLinks.Where(l => l.ContactId == contactId).ToListAsync() ?? new List<ContactLink>();
        }

        public async Task<ContactLink> GetLinkById(int linkId)
        {
            return await DB.ContactLinks.FirstAsync(l => l.Id == linkId);
        }

        public async Task SaveLink(ContactLink link)
        {
            if (link.Id == 0)
                DB.Entry(link).State = EntityState.Added;
            else
                DB.Entry(link).State = EntityState.Modified;

            await DB.SaveChangesAsync();
        }

        public async Task RemoveContactById(int id)
        {
            DB.Remove(new Contact() { Id = id });
            await DB.SaveChangesAsync();
        }

        public async Task RemoveLinkById(int id)
        {
            DB.Remove(new ContactLink() { Id = id });
            await DB.SaveChangesAsync();
        }
    }
}
