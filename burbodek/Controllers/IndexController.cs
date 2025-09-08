using Microsoft.AspNetCore.Mvc;

namespace burbodek.Controllers
{
    public class IndexController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult SignIn()
        {
            return View();
        }
        public IActionResult SignUpClient()
        {
            return View();
        }
        public IActionResult SignUpEmployer()
        {
            return View();
        }
    }
}
