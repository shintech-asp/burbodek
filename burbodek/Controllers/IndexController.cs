using burbodek.Data;
using burbodek.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace burbodek.Controllers
{
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
        public IActionResult BrowseJob()
        {
            return View();
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
                    new Claim("SubscriberType", _context.Subscription.Where(u => u.Status == "Current" && u.UsersId == user.Id).FirstOrDefault()?.PlansId.ToString() ?? "Expired")
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

                // ✅ Redirect to Sign In page
                return RedirectToAction("SignIn", "Index");
            }

            return View(user);
        }
        public IActionResult SignUpEmployer()
        {
            return View();
        }
        [HttpPost]
        public IActionResult SignUpEmployer(EmployerDetails employer, IFormFile? sec_dti, IFormFile? bir_certificate, IFormFile? business_permit, IFormFile? poea_license, IFormFile? proof_partnership)
        {
            ModelState.Remove("Users.Role");
            ModelState.Remove("Users");
            ModelState.Remove("Subscription");
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == employer.Users.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(employer);
                }
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

                var subscriptionDetails = new Subscription
                {
                    UsersId = user.Id,
                    PlansId = 1
                };
                _context.Subscription.Add(subscriptionDetails);
                _context.SaveChanges();

                if (sec_dti != null)
                {
                    using var ms = new MemoryStream();
                    sec_dti.CopyTo(ms);

                    // Detect file type
                    var contentType = sec_dti.ContentType;              // e.g. "image/jpeg", "application/pdf"
                    var extension = Path.GetExtension(sec_dti.FileName); // e.g. ".jpg", ".pdf"

                    var file = new Files
                    {
                        File = ms.ToArray(),
                        UsersId = user.Id,
                        ImageDetails = "sec_dti",
                        FileName = sec_dti.FileName,
                        ContentType = contentType
                    };

                    _context.Files.Add(file);
                    _context.SaveChanges();
                }
                if (bir_certificate != null)
                {
                    using var ms = new MemoryStream();
                    bir_certificate.CopyTo(ms);

                    // Detect file type
                    var contentType = bir_certificate.ContentType;
                    var extension = Path.GetExtension(bir_certificate.FileName);

                    var file = new Files
                    {
                        File = ms.ToArray(),
                        UsersId = user.Id,
                        ImageDetails = "bir_certificate",
                        FileName = bir_certificate.FileName,
                        ContentType = contentType
                    };
                    _context.Files.Add(file);
                    _context.SaveChanges();
                }
                if (business_permit != null)
                {
                    using var ms = new MemoryStream();
                    business_permit.CopyTo(ms);

                    // Detect file type
                    var contentType = business_permit.ContentType;
                    var extension = Path.GetExtension(business_permit.FileName);

                    var file = new Files
                    {
                        File = ms.ToArray(),
                        UsersId = user.Id,
                        ImageDetails = "business_permit",
                        FileName = business_permit.FileName,
                        ContentType = contentType
                    };
                    _context.Files.Add(file);
                    _context.SaveChanges();
                }
                if (poea_license != null)
                {
                    using var ms = new MemoryStream();
                    poea_license.CopyTo(ms);

                    // Detect file type
                    var contentType = poea_license.ContentType;
                    var extension = Path.GetExtension(poea_license.FileName);

                    var file = new Files
                    {
                        File = ms.ToArray(),
                        UsersId = user.Id,
                        ImageDetails = "poea_license",
                        FileName = poea_license.FileName,
                        ContentType = contentType
                    };
                    _context.Files.Add(file);
                    _context.SaveChanges();
                }
                if (proof_partnership != null)
                {
                    using var ms = new MemoryStream();
                    proof_partnership.CopyTo(ms);

                    // Detect file type
                    var contentType = proof_partnership.ContentType;
                    var extension = Path.GetExtension(proof_partnership.FileName);

                    var file = new Files
                    {
                        File = ms.ToArray(),
                        UsersId = user.Id,
                        ImageDetails = "proof_partnership",
                        FileName = proof_partnership.FileName,
                        ContentType = contentType
                    };
                    _context.Files.Add(file);
                    _context.SaveChanges();
                }
                return RedirectToAction("SignIn", "Index");
            }
            return View(employer);
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Index");
        }
    }
}
