using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Areas.Admin.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FundManagementController : Controller
    {
        private readonly FundRepository _fundRepository;

        public FundManagementController(FundRepository fundRepository) 
        {
            _fundRepository = fundRepository;   
        }

        public async Task<IActionResult> Index()
        {
            FundManagementViewModel model = new FundManagementViewModel();
            model.FundTransactions = await _fundRepository.GetLastTransaction(25);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(FundManagementViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            switch (model.Filter)
            {
                case FundManagementViewModel.FilterBy.DATE:
                    model.FundTransactions = await _fundRepository.GetTransactionsByDate(model.DateStart, model.DateEnd);
                    break;

                case FundManagementViewModel.FilterBy.LAST:
                    model.FundTransactions = await _fundRepository.GetLastTransaction(model.CountTransactions);
                    break;
            }   
            return View(model);
        }

        public async Task<IActionResult> TransactionDetails(int? transactionId)
        {
            ViewBag.SelectPurposeItems = await _fundRepository.GetPurposesSelectList();
            if (transactionId == null) 
                return View(new FundTransaction());
            return View(await _fundRepository.GetTransactionById(transactionId.Value));
        }

        [HttpPost]
        public async Task<IActionResult> TransactionDetails(FundTransaction model)
        {
            if (!ModelState.IsValid) return View(model);
            await _fundRepository.SaveTransaction(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemoveTransaction(int transactionId)
        {
            await _fundRepository.RemoveTransactionById(transactionId);
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> TransactionPurposesIndex()
        {
            TransactionPurposesIndexViewModel model = new TransactionPurposesIndexViewModel()
            {
                TransactionPurposes = await _fundRepository.GetAllTransactionPurposes()
            };
            return View(model);
        }

        public async Task<IActionResult> TransactionPurposeDetails(int? purposeId)
        {
            if (purposeId == null)
                return View(new TransactionPurpose());
            return View(await _fundRepository.GetTransactionPurposeById(purposeId.Value));
        }

        [HttpPost]
        public async Task<IActionResult> TransactionPurposeDetails(TransactionPurpose model)
        {
            if (!ModelState.IsValid) return View(model);
            await _fundRepository.SaveTransactionPurpose(model);
            return RedirectToAction(nameof(TransactionPurposesIndex));
        }

        public async Task<IActionResult> RemoveTransactionPurpose(int purposeId)
        {
            if (await _fundRepository.IsPurposeEmpty(purposeId))
                await _fundRepository.RemoveTransactionPurposeById(purposeId);
            else
                TempData["RemoveError"] = $"Ошибка удаления - к назначению с ID {purposeId} привязаны транзакции";
            return RedirectToAction(nameof(TransactionPurposesIndex));
        }
    }
}
