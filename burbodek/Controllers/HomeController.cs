using burbodek.Data;
using burbodek.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Users()
        {
            return View();
        }
        public IActionResult Application()
        {
            var application = _context.EmployerDetails
                .Include(e => e.Users)
                .Include(e => e.Subscription)
                .ToList();

            foreach (var emp in application)
            {
                emp.Subscription = _context.Subscription
                    .Where(s => s.UsersId == emp.UsersId)
                    .Include(e => e.Plans)
                    .ToList();
            }

            return View(application);
        }

        public IActionResult ApplicationDetails(int id)
        {
            var employer = _context.EmployerDetails
                .Include(e => e.Users)
                .Include(e => e.Subscription)
                .Include(e => e.Files)
                .FirstOrDefault(e => e.Id == id);

            employer.Files = _context.Files
                .Where(f => f.UsersId == employer.UsersId)
                .ToList();
            employer.Subscription = _context.Subscription
                .Where(s => s.UsersId == employer.UsersId)
                .Include(e => e.Plans)
                .ToList();
            if (employer == null) return NotFound();

            return View(employer);
        }

        public IActionResult DownloadDoc(int docId)
        {
            var doc = _context.Files.Find(docId);
            if (doc == null) return NotFound();

            return File(doc.File, doc.ContentType, doc.FileName);
        }

        public IActionResult Reports()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpPost]
        public IActionResult ApplicationApproval(int SubscriptionId, int Id, string ApprovalDetails)
        {
            if(ApprovalDetails == "decline")
            {
                var data = _context.Subscription.Where(e => e.Id == SubscriptionId).FirstOrDefault();

                data.Expiration = DateTime.Now;
                _context.Subscription.Update(data);
                var employer = _context.EmployerDetails.Where(u => u.Id == Id).FirstOrDefault();
                employer.Status = "Decline";
                _context.EmployerDetails.Update(employer);
                _context.SaveChanges();
                TempData["success"] = "Employer declined";
                return RedirectToAction("Index");
            }else if(ApprovalDetails == "approve")
            {
                var data = _context.Subscription.Where(e => e.Id == SubscriptionId).FirstOrDefault();

                data.Expiration = DateTime.Now.AddMonths(1);
                _context.Subscription.Update(data);
                var employer = _context.EmployerDetails.Where(u => u.Id == Id).FirstOrDefault();
                employer.Status = "Approved";
                _context.EmployerDetails.Update(employer);
                _context.SaveChanges();
                TempData["success"] = "Employer successfully approved!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Unexpected error occur";
                return RedirectToAction("Application");
            }
        }
    }
}
