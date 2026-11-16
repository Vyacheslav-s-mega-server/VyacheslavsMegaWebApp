using Microsoft.AspNetCore.Mvc;

namespace VyacheslavsMegaWebApp.Controllers
{
    public class FriendsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
