using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VyacheslavsMegaWebApp.Areas.Admin.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Repositories;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactsEditorController : Controller
    {
        private readonly ContactsInfoRepository _contactsInfoRepository;
        private readonly UserManager<VmsIdentityUser> _userManager;

        public ContactsEditorController(ContactsInfoRepository contactsInfoRepository, UserManager<VmsIdentityUser> userManager)
        {
            _contactsInfoRepository = contactsInfoRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ContactsEditorViewModel model = new ContactsEditorViewModel(await _contactsInfoRepository.GetAllContacts());
            return View(model);
        }

        public async Task<IActionResult> ContactDetails(int? contactId) 
        {
            ViewBag.SelectUserItems = _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem(u.UserName, u.Id));
            if (contactId == null)
                return View(new Contact());
            Contact selectedContact = await _contactsInfoRepository.GetContactById(contactId.Value);
            return View(selectedContact);
        }

        [HttpPost]
        public async Task<IActionResult> ContactDetails(Contact model)
        { 
            if (ModelState.IsValid)
            {
                await _contactsInfoRepository.SaveContact(model);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SelectUserItems = _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem(u.UserName, u.Id));
            return View(nameof(ContactDetails), model);
        }

        public async Task<IActionResult> RemoveContact(int contactId)
        {
            await _contactsInfoRepository.RemoveContactById(contactId);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ContactLinks(int contactId)
        {
            ViewBag.ContactId = contactId;
            LinksEditorViewModel<ContactLink> model = new LinksEditorViewModel<ContactLink>(await _contactsInfoRepository.GetContactLinks(contactId));
            return View(model);
        }

        public async Task<IActionResult> LinkDetails(int contactId, int? linkId) 
        {
            ViewBag.SelectContactItems = (await _contactsInfoRepository.GetAllContacts())
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem(c.DisplayName, c.Id.ToString()));
            ViewBag.ContactId = contactId;
            ContactLink link;
            if (linkId == null)
            {
                link = new ContactLink() { ContactId = contactId };
                return View(link);
            }
            link = await _contactsInfoRepository.GetLinkById(linkId.Value);
            return View(link);
        }

        [HttpPost]
        public async Task<IActionResult> LinkDetails(ContactLink model, int contactId)
        {
            if (ModelState.IsValid)
            {
                await _contactsInfoRepository.SaveLink(model);
                RouteValueDictionary routeValues = new RouteValueDictionary()
                {
                    { "contactId", contactId }
                };
                return RedirectToAction(nameof(ContactLinks), routeValues);
            }
            ViewBag.SelectContactItems = (await _contactsInfoRepository.GetAllContacts())
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem(c.DisplayName, c.Id.ToString()));
            return View(model);
        }

        public async Task<IActionResult> RemoveLink(int linkId, int contactId)
        {
            await _contactsInfoRepository.RemoveLinkById(linkId);
            RouteValueDictionary routeValues = new RouteValueDictionary()
            {
                { "contactId", contactId }
            };
            return RedirectToAction(nameof(ContactLinks), routeValues);
        }
    }
}
