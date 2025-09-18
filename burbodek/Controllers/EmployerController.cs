using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Message()
        {
            return View();
        }
        public IActionResult Compose()
        {
            return View();
        }
        public IActionResult Reply()
        {
            return View();
        }
        public IActionResult JobListing()
        {
            return View();
        }
        public IActionResult AccountSettings()
        {
            return View();
        }
        public IActionResult AccountBilling()
        {
            return View();
        }
        public IActionResult AccountOverview()
        {
            return View();
        }
        public IActionResult Billing()
        {
            return View();
        }
        public IActionResult JobCreate()
        {
            return View();
        }
        public IActionResult TrainingCreate()
        {
            return View();
        }
    }
}
