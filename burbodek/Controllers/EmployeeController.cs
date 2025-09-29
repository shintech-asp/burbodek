using burbodek.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Client")]
    public class EmployeeController : Controller
    {
        ApplicationDbContext _context;
        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Jobs
                        .Include(u => u.Users)
                            .ThenInclude(u => u.EmployerDetails)
                        .Include(u => u.JobRequirements)
                        .Include(u => u.JobMedia)
                        .Include(u => u.JobBenefits)
                        .Include(u => u.JobRole)
                        .Where(u => u.ExpirationDate > DateTime.Now)
                        .ToList();
            return View(data);
        }
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult JobInfo(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Jobs
                        .Include(u => u.Users)
                            .ThenInclude(u => u.EmployerDetails)
                        .Include(u => u.JobRequirements)
                        .Include(u => u.JobMedia)
                        .Include(u => u.JobBenefits)
                        .Include(u => u.JobRole)
                        .Where(u => u.Id == Id)
                        .FirstOrDefault();
            return View(data);
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
        public IActionResult JobApply()
        {
            return View();
        }
        public IActionResult Applications()
        {
            return View();
        }
        public IActionResult AccountSettings()
        {
            return View();
        }
    }
}
