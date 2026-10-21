using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting.Internal;
using VyacheslavsMegaWebApp.Areas.Admin.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PartnersEditorController : Controller
    {
        private readonly PartnersInfoRepository _partnersInfoRepository;
        private readonly ImageManager _imageManager;

        public PartnersEditorController(PartnersInfoRepository partnersInfoRepository, ImageManager imageManager)
        {
            _partnersInfoRepository = partnersInfoRepository;
            _imageManager = imageManager;
        }

        public async Task<IActionResult> Index()
        {
            PartnersEditorViewModel model = new PartnersEditorViewModel(await _partnersInfoRepository.GetAllPartners());
            return View(model);
        }

        public async Task<IActionResult> PartnerDetails(int? partnerId) 
        {
            if (partnerId == null)
                return View(new Partner());
            return View(await _partnersInfoRepository.GetPartnerById(partnerId.Value));
        }

        [HttpPost]
        public async Task<IActionResult> PartnerDetails(Partner model, IFormFile? partnerLogoImage)
        {
            if (!ModelState.IsValid) return View(model);

            if (partnerLogoImage != null) 
            {
                string newImageName = await _imageManager.SaveUniqueFileName(Partner.LOGOS_DIR, partnerLogoImage);
                if (!string.IsNullOrEmpty(model.LogoFileName))
                {
                    _imageManager.RemoveImage(Partner.LOGOS_DIR, model.LogoFileName);
                } 
                model.LogoFileName = newImageName;
            }
            await _partnersInfoRepository.SavePartner(model);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemovePartner(int partnerId)
        {
            Partner partnerToRemove = await _partnersInfoRepository.GetPartnerById(partnerId);
            await _partnersInfoRepository.RemovePartnerById(partnerId);
            _imageManager.RemoveImage(Partner.LOGOS_DIR, partnerToRemove.LogoFileName);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> PartnerLinks(int partnerId)
        {
            ViewBag.PartnerId = partnerId;
            LinksEditorViewModel<PartnerLink> model = new LinksEditorViewModel<PartnerLink>(await _partnersInfoRepository.GetPartnerLinks(partnerId));
            return View(model);
        }

        public async Task<IActionResult> LinkDetails(int partnerId, int? linkId)
        {
            ViewBag.SelectPartnerItems = (await _partnersInfoRepository.GetAllPartners())
                .OrderBy(p => p.Id)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()));
            ViewBag.PartnerId = partnerId;
            PartnerLink link;
            if (linkId == null)
            {
                link = new PartnerLink() { PartnerId = partnerId };
                return View(link);
            }
            link = await _partnersInfoRepository.GetLinkById(linkId.Value);
            return View(link);
        }

        [HttpPost]
        public async Task<IActionResult> LinkDetails(PartnerLink model, int partnerId)
        {
            if (ModelState.IsValid)
            {
                await _partnersInfoRepository.SaveLink(model);
                RouteValueDictionary routeValues = new RouteValueDictionary()
                {
                    { "partnerId", partnerId }
                };
                return RedirectToAction(nameof(PartnerLinks), routeValues);
            }
            ViewBag.SelectPartnerItems = (await _partnersInfoRepository.GetAllPartners())
                .OrderBy(p => p.Id)
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()));
            return View(model);
        }

        public async Task<IActionResult> RemoveLink(int linkId, int partnerId)
        {
            await _partnersInfoRepository.RemoveLinkById(linkId);
            RouteValueDictionary routeValues = new RouteValueDictionary()
            {
                { "partnerId", partnerId }
            };
            return RedirectToAction(nameof(PartnerLinks), routeValues);
        }
    }
}
