using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Areas.Admin.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class GoalManagementController : Controller
    {
        private readonly GoalRepository _goalRepository;
        private readonly ImageManager _imageManager;

        public GoalManagementController(GoalRepository goalRepository, ImageManager imageManager) 
        {
            _goalRepository = goalRepository;
            _imageManager = imageManager;
        }

        public async Task<IActionResult> Index()
        {
            GoalManagementViewModel model = new GoalManagementViewModel();
            model.Goals = await _goalRepository.GetAllGoals();
            return View(model);
        }

        public async Task<IActionResult> GoalDetails(int? goalId)
        {
            if (goalId == null)
                return View(new Goal());
            return View(await _goalRepository.GetGoalById(goalId.Value));
        }

        [HttpPost]
        public async Task<IActionResult> GoalDetails(Goal model, IFormFile? goalLogoImage)
        {
            if (!ModelState.IsValid) return View(model);

            if (goalLogoImage != null)
            {
                string newImageName = await _imageManager.SaveUniqueFileName(Goal.LOGOS_DIR, goalLogoImage);
                if (!string.IsNullOrEmpty(model.LogoFileName))
                {
                    _imageManager.RemoveImage(Goal.LOGOS_DIR, model.LogoFileName);
                }
                model.LogoFileName = newImageName;
            }
            await _goalRepository.SaveGoal(model);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemoveGoal(int goalId)
        {
            Goal goalToRemove = await _goalRepository.GetGoalById(goalId);
            await _goalRepository.RemoveGoalById(goalId);
            if (goalToRemove.HasLogo)
                _imageManager.RemoveImage(Goal.LOGOS_DIR, goalToRemove.LogoFileName);
            return RedirectToAction(nameof(Index));
        }
    }
}
