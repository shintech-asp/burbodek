using burbodek.Data;
using burbodek.Models;
using burbodek.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController : Controller
    {
        ApplicationDbContext _context;
        public EmployerController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Subscription.Where(u => u.UsersId == userId && u.Expiration > DateTime.Now && u.Status == "Current").FirstOrDefault();
            return View(data);
        }

        public IActionResult Subscription()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var userDetails = _context.Users
                                .Include(u => u.EmployerDetails)
                                .Include(u => u.PaymentDetails)
                                .Where(u => u.Id == userId)
                                .FirstOrDefault();
            var plansDetails = _context.Plans
                                        .Where(u => u.Id != 1)
                                        .ToList();

            var SubscriptionData = new SubscriptionViewModel
            {
                UserDetails = userDetails,
                PlansDetails = plansDetails
            };
            return View(SubscriptionData);
        }
        public IActionResult PaymentDetails()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.PaymentDetails.Where(u => u.UsersId == userId).ToList();
            return View(data);
        }

        [HttpPost]
        public IActionResult PaymentDetails(PaymentDetails payment)
        {
            ModelState.Remove("Users"); // ignore navigation property validation
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            if (ModelState.IsValid)
            {
                var paymentDetails = new PaymentDetails
                {
                    UsersId = userId,
                    PhoneNumber = payment.PhoneNumber,
                    Name = payment.Name
                };

                _context.PaymentDetails.Add(paymentDetails);
                _context.SaveChanges();

                return RedirectToAction("PaymentDetails");
            }

            return RedirectToAction("PaymentDetails");
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
