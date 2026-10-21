using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VyacheslavsMegaWebApp.Data.JSON.Repositories;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly MainPageRepository _mainPageRepository;
        private readonly PartnersInfoRepository _partnersInfoRepository;
        private readonly ContactsInfoRepository _contactsInfoRepository;
        private readonly DonationCardsRepository _cardsRepository;
        private readonly ServerMediaLinksRepository _mediaLinksRepository;
        private readonly FundRepository _fundRepository;
        private readonly GoalRepository _goalRepository;
        private readonly MCServerStatRepository _MCServerStatRepository;
        private readonly DownloadButtonsRepository _downloadButtonsRepository;

        public HomeController(
            MainPageRepository mainPageRepository, 
            PartnersInfoRepository partnersInfoRepository, 
            ContactsInfoRepository contactsInfoRepository, 
            DonationCardsRepository cardsRepository, 
            ServerMediaLinksRepository mediaLinksRepository, 
            FundRepository fundRepository, 
            GoalRepository goalRepository,
            MCServerStatRepository serverStatRepository,
            DownloadButtonsRepository buttonsRepository
            )
        {
            _MCServerStatRepository = serverStatRepository;
            _mainPageRepository = mainPageRepository;
            _contactsInfoRepository = contactsInfoRepository;
            _partnersInfoRepository = partnersInfoRepository;
            _cardsRepository = cardsRepository;
            _mediaLinksRepository = mediaLinksRepository;
            _fundRepository = fundRepository;
            _goalRepository = goalRepository;
            _downloadButtonsRepository = buttonsRepository;
        }

        public async Task<IActionResult> Index()
        {
            MainPageViewModel model = new MainPageViewModel()
            {
                MainPageData = await _mainPageRepository.GetMainPageData(),
                DownloadButtons = await _downloadButtonsRepository.GetButtonsToShow(),
                MCServerStat = await _MCServerStatRepository.GetMCServerStats(),
                Creator = await _contactsInfoRepository.GetContactById(1),
                PartnersData = await _partnersInfoRepository.GetAllPartnersRandomly(),
                MediaLinks = await _mediaLinksRepository.GetAllLinks(),
            };
            ViewBag.PageTitle = model.MainPageData.PageTitle;
            ViewBag.Keywords = model.MainPageData.MetatagKeywords;
            ViewBag.Description = model.MainPageData.MetatagDescription;
            return View(model);
        }

        public async Task<IActionResult> DonationPage() 
        {
            DonationPageViewModel model = new DonationPageViewModel()
            {
                DonationCards = await _cardsRepository.GetAllCards(),
                Goal = await _goalRepository.GetCurrentGoal(),
                FundAmount = await _fundRepository.GetFundState(),
                LastTransactions = await _fundRepository.GetLastTransaction(3)
            };
            return View(model);
        }

        public async Task<IActionResult> TransactionsHistory()
        {
            TransactionsHistoryViewModel model = new TransactionsHistoryViewModel()
            {
                TransactionsList = await _fundRepository.GetLastTransaction(10),
                MaxIncome = _fundRepository.GetMaxByType(Data.Entities.TransactionPurpose.TransactionType.Income),
                MaxExpence = _fundRepository.GetMaxByType(Data.Entities.TransactionPurpose.TransactionType.Expense),
                TotalCount = _fundRepository.GetTotalTransactionsCount()
            };
            return View(model);
        }

        public async Task<IActionResult> GetTransactions(int lastId)
        {
            return Json(await _fundRepository.GetTransactionsAfterId(lastId, 50));
        }

        public async Task<IActionResult> Contacts()
        {
            ViewBag.PageTitle = "Контакты - Vyacheslav's Mega Server";
            var contacts = await _contactsInfoRepository.GetAllContacts();
            return View(contacts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel(ErrorViewModel.PageType.Exception) 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCode(int code)
        {
            return View("Error", new ErrorViewModel(ErrorViewModel.PageType.StatusCode)
            {
                StatusCode = code,
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
