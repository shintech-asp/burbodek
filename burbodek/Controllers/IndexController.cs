using Microsoft.AspNetCore.Mvc;

namespace burbodek.Controllers
{
    public class IndexController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult SignIn(int? id)
        {
            return View(id);
        }
        public IActionResult SignUpClient(int? id)
        {
            return View(id);
        }
        public IActionResult SignUpEmployer(int? id)
        {
            return View(id);
        }
    }
}
