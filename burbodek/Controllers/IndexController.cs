using burbodek.Data;
using burbodek.Filters;
using burbodek.Models;
using burbodek.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace burbodek.Controllers
{
    [RedirectIfAuthenticated]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class IndexController : Controller
    {
        ApplicationDbContext _context;
        public IndexController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult BrowseJob(string keyword, string location, int page = 1)
        {
            int pageSize = 10;

            // --- JOBS QUERY ---
            var jobQuery = _context.Jobs
                .Include(j => j.Users)
                    .ThenInclude(u => u.EmployerDetails)
                .Include(j => j.JobApplication)
                .Where(j => j.ExpirationDate > DateTime.Now && j.isArchived == null);

            if (!string.IsNullOrEmpty(keyword))
            {
                jobQuery = jobQuery.Where(j =>
                    j.JobTitle.Contains(keyword) ||
                    j.JobDescription.Contains(keyword) ||
                    j.JobRole.Any(r => r.Role.Contains(keyword)));
            }

            if (!string.IsNullOrEmpty(location))
            {
                jobQuery = jobQuery.Where(j => j.Users.EmployerDetails.Address.Contains(location));
            }

            int totalJobs = jobQuery.Count();

            var jobs = jobQuery
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(j => new JobItemViewModel
                {
                    Id = j.Id,
                    JobTitle = j.JobTitle,
                    JobDescription = j.JobDescription,
                    EmployerAddress = j.Users.EmployerDetails.Address,
                    SalaryMin = j.SalaryMin,
                    SalaryMax = j.SalaryMax,
                    CreatedAt = j.CreatedAt
                })
                .AsNoTracking()
                .ToList();

            // --- TRAININGS QUERY ---
            var trainingQuery = _context.Training
                .Include(t => t.Users)
                    .ThenInclude(u => u.EmployerDetails)
                .Include(t => t.TrainingApplication)
                .Where(t => t.isArchived == null);

            if (!string.IsNullOrEmpty(keyword))
            {
                trainingQuery = trainingQuery.Where(t =>
                    t.Name.Contains(keyword) || t.TrainingDescription.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(location))
            {
                trainingQuery = trainingQuery.Where(t => t.Users.EmployerDetails.Address.Contains(location));
            }

            var trainings = trainingQuery
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TrainingItemViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    TrainingDescription = t.TrainingDescription,
                    EmployerAddress = t.Users.EmployerDetails.Address,
                    Price = t.Price,
                    ModeOfPayment = t.ModeOfPayment,
                    PaymentOption = t.PaymentOption,
                    CreatedAt = t.CreatedAt
                })
                .AsNoTracking()
                .ToList();

            // --- COMBINE INTO ONE VIEWMODEL ---
            var viewModel = new JobListViewModel
            {
                Jobs = jobs,
                Trainings = trainings,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalJobs / (double)pageSize),
                Keyword = keyword,
                Location = location
            };

            return View(viewModel);
        }
        public IActionResult SignIn()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SignIn(string email, string password)
        {
            var user = _context.Users
                        .Include(u => u.EmployerDetails)
                        .FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "No account found with this email.");
                return View();
            }

            var passwordHasher = new PasswordHasher<Users>();
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Success)
            {
                var claims = new List<Claim>
                {
                    new Claim("UsersId", user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("Status", user.EmployerDetails?.Status ?? "none"),
                    new Claim("isSubscriber", _context.Subscription.Any(s => s.UsersId == user.Id && s.Status == "Current").ToString()),
                    new Claim("SubscriberType", _context.Subscription.Where(u => u.Status == "Current" && u.UsersId == user.Id).FirstOrDefault()?.PlansId.ToString() ?? "Expired"),
                    new Claim("Plan", _context.Subscription.Include(u=>u.Plans).Where(u => u.Status == "Current" && u.UsersId == user.Id).FirstOrDefault()?.Plans.PlanName.ToString() ?? "None"),
                    new Claim("isTrainingCenter", _context.EmployerDetails.Any(u => u.UsersId == user.Id && u.isTrainingCenter == 1).ToString()),
                    new Claim("isEmployer", _context.EmployerDetails.Any(u => u.UsersId == user.Id && u.isEmployer == 1).ToString()),
                };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("MyCookieAuth", principal);

                // 🔹 Redirect based on role
                switch (user.Role)
                {
                    case "Admin":
                        return RedirectToAction("Index", "Home");
                    case "Client":
                        return RedirectToAction("Index", "Employee");
                    case "Employer":
                        return RedirectToAction("Index", "Employer");
                    case "Trainer":
                        return RedirectToAction("Index", "Seller");
                }
            }
            ModelState.AddModelError("Password", "Incorrect password.");
            return View();
        }
        public IActionResult SignUpClient() 
        {
            return View();
        }
        [HttpPost]
        public IActionResult SignUpClient(Users user)
        {
            // Remove Role from validation if it's not set by the form
            ModelState.Remove("Role");

            // ✅ Basic null/empty checks
            if (string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(user);
                }

                // Assign default role
                user.Role = "Client";
                user.DateCreated = DateTime.Now;

                // ✅ Hash the password
                var passwordHasher = new PasswordHasher<Users>();
                user.Password = passwordHasher.HashPassword(user, user.Password);

                // ✅ Save user to DB
                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("SignIn", "Index");
            }

            return View(user);
        }

        public IActionResult SignUpEmployer()
        {
            return View();
        }
        [HttpPost]
        public IActionResult SignUpEmployer(
    EmployerDetails employer,
    IFormFile? sec_dti,
    IFormFile? bir_certificate,
    IFormFile? business_permit,
    IFormFile? poea_license,
    IFormFile? proof_partnership)
        {
            ModelState.Remove("Users.Role");
            ModelState.Remove("Users");
            ModelState.Remove("Subscription");

            // ✅ Basic validation
            if (employer.Users == null ||
                string.IsNullOrWhiteSpace(employer.Users.Username) ||
                string.IsNullOrWhiteSpace(employer.Users.Email) ||
                string.IsNullOrWhiteSpace(employer.Users.Password) ||
                string.IsNullOrWhiteSpace(employer.BusinessName) ||
                string.IsNullOrWhiteSpace(employer.Address))
            {
                TempData["Error"] = "Please fill in all required fields.";
            }

            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == employer.Users.Email))
                {
                    TempData["Error"] = "Email is already registered.";
                    return View(employer);
                }

                // ✅ Create user
                var user = new Users
                {
                    Username = employer.Users.Username,
                    Email = employer.Users.Email,
                    Password = employer.Users.Password,
                    Role = "Employer",
                    DateCreated = DateTime.Now
                };

                var passwordHasher = new PasswordHasher<Users>();
                user.Password = passwordHasher.HashPassword(user, user.Password);
                _context.Users.Add(user);
                _context.SaveChanges();

                // ✅ Employer details
                var employerDetails = new EmployerDetails
                {
                    UsersId = user.Id,
                    isEmployer = employer.isEmployer ?? 0,
                    isTrainingCenter = employer.isTrainingCenter ?? 0,
                    BusinessName = employer.BusinessName,
                    BusinessDescription = employer.BusinessDescription,
                    Address = employer.Address,
                    Latitude = employer.Latitude,
                    Longitude = employer.Longitude
                };
                _context.EmployerDetails.Add(employerDetails);

                // ✅ Default subscription
                var subscriptionDetails = new Subscription
                {
                    UsersId = user.Id,
                    PlansId = 1
                };
                _context.Subscription.Add(subscriptionDetails);
                _context.SaveChanges();

                // ✅ File uploads
                void SaveFile(IFormFile? file, string details)
                {
                    if (file != null)
                    {
                        // Ensure uploads directory exists
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Unique filename to avoid collisions
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save file physically
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }

                        // Save metadata in DB
                        var newFile = new Files
                        {
                            UsersId = user.Id,
                            ImageDetails = details,
                            FileName = file.FileName, // original name
                            ContentType = file.ContentType,
                            File = $"/uploads/{uniqueFileName}" // relative path for serving
                        };

                        _context.Files.Add(newFile);
                        _context.SaveChanges();
                    }
                }

                SaveFile(sec_dti, "sec_dti");
                SaveFile(bir_certificate, "bir_certificate");
                SaveFile(business_permit, "business_permit");
                SaveFile(poea_license, "poea_license");
                SaveFile(proof_partnership, "proof_partnership");

                TempData["Success"] = "Email successfully registered!";
                return RedirectToAction("SignIn", "Index");
            }
            TempData["Error"] = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return View(employer);
        }

    }
}
