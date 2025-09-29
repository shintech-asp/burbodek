using burbodek.Data;
using burbodek.Models;
using burbodek.Models.ViewModels;
using burbodek.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Security.Claims;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController : Controller
    {
        ApplicationDbContext _context;
        private readonly IPaymongo _paymongo;
        public EmployerController(ApplicationDbContext context, IPaymongo paymongo)
        {
            _context = context;
            _paymongo = paymongo;
        }
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Subscription.Where(u => u.UsersId == userId && (u.Expiration > DateTime.Now || !u.Expiration.HasValue) && u.Status == "Current").FirstOrDefault();
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
        [HttpPost]
        public async Task<IActionResult> Checkout(decimal amount, string planName, string email, string contact, string username)
        {
            try
            {
                // ✅ Basic validation
                if (amount <= 0 || string.IsNullOrWhiteSpace(planName) ||
                    string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(contact) ||
                    string.IsNullOrWhiteSpace(username))
                {
                    // Return back with validation error
                    TempData["Error"] = "Invalid checkout details. Please make sure all fields are filled correctly.";
                    return RedirectToAction("Subscription", "Employer"); // 👈 Change to your subscription page
                }

                // ✅ Create checkout session
                var responseJson = await _paymongo.CreateCheckoutSession(
                    amount,
                    "PHP",
                    username,
                    email,
                    contact,
                    planName
                );

                // ✅ Parse response
                var json = JObject.Parse(responseJson);
                var checkoutUrl = json["data"]?["attributes"]?["checkout_url"]?.ToString();

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    TempData["Error"] = "Failed to retrieve checkout URL. Please try again later.";
                    return RedirectToAction("Subscription", "Employer");
                }

                TempData["PlanName"] = planName;
                TempData["Amount"] = (int)amount;

                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Checkout failed: {ex.Message}";
                return RedirectToAction("Subscription", "Employer");
            }
        }

        public IActionResult CancelledPayment()
        {
            return View();
        }
        public async Task<IActionResult> SuccessPayment()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            // Example: values you might pass via TempData or querystring
            var planName = TempData["PlanName"]?.ToString();
            int amount = TempData["Amount"] != null ? Convert.ToInt32(TempData["Amount"]) : 0;
            var getUserSub = _context.Subscription.Where(u => u.UsersId == userId && u.Status == "Current").FirstOrDefault();
            var plan = _context.Plans.FirstOrDefault(p => p.PlanName == planName);
            var expiration = new DateTime();
            if (getUserSub.PlansId == 1)
            {
                if (plan.Id == 2)
                {
                    getUserSub.Status = "Expired";
                    expiration = DateTime.Now.AddMonths(1);
                }
                else
                {
                    getUserSub.Status = "Expired";
                    expiration = DateTime.Now.AddYears(1);
                }
            }
            else
            {
                if (plan.Id == 2)
                {
                    getUserSub.Status = "Renewed";
                    expiration = getUserSub.Expiration.Value.AddMonths(1);
                }
                else
                {
                    getUserSub.Status = "Renewed";
                    expiration = getUserSub.Expiration.Value.AddYears(1);
                }
            }
            _context.Subscription.Update(getUserSub);
            _context.SaveChanges();
            if (string.IsNullOrEmpty(planName) || amount <= 0)
            {
                // fallback if data wasn’t passed
                return RedirectToAction("Index", "Home");
            }

            var subscription = new Subscription
            {
                UsersId = userId,
                PlansId = plan.Id,
                Expiration = expiration,
                Status = "Current",
            };
            _context.Subscription.Add(subscription);
            var payment = new Payments
            {
                Amount = amount,
                PaymentDetails = planName + " Subscription",
                Status = "Paid",
                UsersId = userId,
            };
            _context.Payments.Add(payment);
            _context.SaveChanges();
            // after saving subscription changes
            await HttpContext.SignOutAsync("MyCookieAuth"); // clear old cookie

            var user = _context.Users
                        .Include(u => u.EmployerDetails)
                        .FirstOrDefault(u => u.Id == userId);
            var claims = new List<Claim>
            {
                new Claim("UsersId", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Status", user.EmployerDetails?.Status ?? "none"),
                new Claim("isSubscriber", _context.Subscription.Any(s => s.UsersId == user.Id && s.Status == "Current").ToString()),
                new Claim("SubscriberType", _context.Subscription
                    .Where(u => u.Status == "Current" && u.UsersId == user.Id)
                    .Select(s => s.PlansId.ToString())
                    .FirstOrDefault() ?? "Expired"),
                new Claim("Plan", _context.Subscription
                    .Include(s => s.Plans)
                    .Where(s => s.Status == "Current" && s.UsersId == user.Id)
                    .Select(s => s.Plans.PlanName)
                    .FirstOrDefault() ?? "None"),
                new Claim("isTrainingCenter", _context.EmployerDetails.Any(u => u.UsersId == user.Id && u.isTrainingCenter == 1).ToString()),
                new Claim("isEmployer", _context.EmployerDetails.Any(u => u.UsersId == user.Id && u.isEmployer == 1).ToString()),
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("MyCookieAuth", principal);

            return View(subscription); // you can show details in success view
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
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Jobs
                        .Include(u => u.JobBenefits)
                        .Include(u => u.JobRequirements)
                        .Include(u => u.JobRole)
                        .Include(u => u.JobMedia)
                        .Where(u => u.UsersId == userId)
                        .ToList();
            return View(data);
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
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var user = _context.Users
                .Include(u => u.EmployerDetails)
                .Include(u => u.Payments)
                .Include(u => u.Subscription.Where(s => s.Status == "Current"))
                    .ThenInclude(s => s.Plans)
                .Include(u => u.PaymentDetails)
                .Where(u => u.Id == userId)
                .FirstOrDefault();

            if (user == null)
                return NotFound();

            return View(user);
        }

        public IActionResult JobCreate()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> JobCreate(JobCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fill up all the details.";
                return View(model);
            }

            var job = new Jobs
            {
                UsersId = int.Parse(User.FindFirst("UsersId")?.Value),
                JobTitle = model.JobTitle,
                JobType = model.JobType,
                SalaryMin = model.SalaryMin,
                SalaryMax = model.SalaryMax,
                ExpirationDate = model.ExpirationDate,
                JobDescription = model.JobDescription
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            // Requirements (many)
            foreach (var requirement in model.JobRequirements ?? Enumerable.Empty<string>())
            {
                var JobRequirements = new JobRequirements
                {
                    JobsId = job.Id,
                    Requirement = requirement
                };
                _context.JobRequirements.Add(JobRequirements);
            }
            // Roles (many)
            foreach (var role in model.JobRole ?? Enumerable.Empty<string>())
            {
                var JobRole = new JobRole
                {
                    JobsId = job.Id,
                    Role = role
                };
                _context.JobRole.Add(JobRole);
            }

            // Benefits (many)
            foreach (var benefit in model.JobBenefits ?? Enumerable.Empty<string>())
            {
                var JobBenefits = new JobBenefits
                {
                    JobsId = job.Id,
                    Benefit = benefit
                };
                _context.JobBenefits.Add(JobBenefits);
            }

            // Media (many)
            foreach (var file in model.JobMedia ?? Enumerable.Empty<IFormFile>())
            {
                if (file.Length > 0)
                {
                    // Generate unique file name (to avoid conflicts)
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(uploadPath)!);

                    // Save file to disk
                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Save only path + type in DB
                    var JobMedia = new JobMedia
                    {
                        JobsId = job.Id,
                        FilePath = $"/uploads/{fileName}",
                        FileType = file.ContentType
                    };
                    _context.JobMedia.Add(JobMedia);
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult TrainingCreate()
        {
            return View();
        }
    }
}
