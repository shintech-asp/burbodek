using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace burbodek.Controllers
{
    public class AccountController : Controller
    {
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Index");
        }
    }
}
