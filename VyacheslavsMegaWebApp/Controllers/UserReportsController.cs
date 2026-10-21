using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Controllers
{
    public class UserReportsController : Controller
    {
        private readonly UserReportsRepository _reportsRepository;

        public UserReportsController(UserReportsRepository reportsRepository) 
        { 
            _reportsRepository = reportsRepository;
        }

        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> New(UserReport model)
        {
            if (ModelState.IsValid)
            {
                await _reportsRepository.SaveReportAsync(model);
                return Redirect(nameof(SuccessSending));
            }
            return View(model);
        }

        public IActionResult SuccessSending()
        {
            return View();
        }

        [HttpPost("api/reports/submit")]
        public async Task<IActionResult> ApiSubmitReport([FromBody] UserReport model)
        {
            if (string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.ReportText))
            {
                return BadRequest(new { error = "Тема и описание обязательны." });
            }
            await _reportsRepository.SaveReportAsync(model);
            return Ok(new { success = true, id = model.Id });
        }
    }
}
