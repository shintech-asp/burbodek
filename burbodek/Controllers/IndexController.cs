using burbodek.Data;
using burbodek.Filters;
using burbodek.Models;
using burbodek.Models.ViewModels;
using burbodek.Services;
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
        private readonly EmailServices _email;
        public IndexController(ApplicationDbContext context, EmailServices email)
        {
            _context = context;
            _email = email;
        }
        public IActionResult Index()
        {
            var data = _context.Faq.ToList();
            var FaqTitle = _context.FaqTitle.OrderByDescending(u => u.Id).FirstOrDefault();

            var model = new FAQViewModel
            {
                Faqs = data,
                FaqTitle = FaqTitle
            };
            return View(model);
        }

        public IActionResult BrowseJob(string keyword, string location, decimal? salaryMin, decimal? salaryMax, int page = 1, string filter = "all")
        {
            int pageSize = 10;

            // Normalize filter
            filter = string.IsNullOrEmpty(filter) ? "all" : filter.ToLower();

            List<JobItemViewModel> jobs = new();
            List<TrainingItemViewModel> trainings = new();
            int totalJobs = 0;
            int totalTrainings = 0;

            // --- JOBS QUERY (skip if filter is training) ---
            if (filter == "all" || filter == "jobs")
            {
                var jobQuery = _context.Jobs
                    .Include(j => j.Users)
                        .ThenInclude(u => u.EmployerDetails)
                    .Include(j => j.JobApplication)
                    .Where(j => j.ExpirationDate > DateTime.Now && j.isArchived == null && j.isDeleted == null && (j.JobApplication.Count(u => u.Status == "Hired") < j.WillHire));

                if (!string.IsNullOrEmpty(keyword))
                {
                    jobQuery = jobQuery.Where(j =>
                        j.JobTitle.Contains(keyword) ||
                        j.JobDescription.Contains(keyword) ||
                        j.JobRole.Any(r => r.Role.Contains(keyword)));
                }

                if (!string.IsNullOrEmpty(location))
                    jobQuery = jobQuery.Where(j => j.Users.EmployerDetails.Address.Contains(location));

                if (salaryMin.HasValue)
                    jobQuery = jobQuery.Where(j => j.SalaryMax >= salaryMin.Value);

                if (salaryMax.HasValue)
                    jobQuery = jobQuery.Where(j => j.SalaryMin <= salaryMax.Value);

                totalJobs = jobQuery.Count();

                jobs = jobQuery
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
                        CreatedAt = j.CreatedAt,
                        JobRequiredBadge = j.JobRequiredBadge.ToList(),
                        AlreadyApplied = false
                    })
                    .AsNoTracking()
                    .ToList();
            }

            // --- TRAININGS QUERY (skip if filter is job) ---
            if (filter == "all" || filter == "trainings")
            {
                var trainingQuery = _context.Training
                    .Include(t => t.Users)
                        .ThenInclude(u => u.EmployerDetails)
                    .Include(t => t.TrainingApplication)
                    .Where(t => t.isArchived == null && t.Expiration >= DateTime.Now && t.isDeleted == null);

                if (!string.IsNullOrEmpty(keyword))
                {
                    trainingQuery = trainingQuery.Where(t =>
                        t.Name.Contains(keyword) || t.TrainingDescription.Contains(keyword));
                }

                if (!string.IsNullOrEmpty(location))
                    trainingQuery = trainingQuery.Where(t => t.Users.EmployerDetails.Address.Contains(location));

                // Salary filter only applies to jobs, but if you want price range for trainings too:
                if (salaryMin.HasValue)
                    trainingQuery = trainingQuery.Where(t => t.Price >= salaryMin.Value);

                if (salaryMax.HasValue)
                    trainingQuery = trainingQuery.Where(t => t.Price <= salaryMax.Value);

                totalTrainings = trainingQuery.Count();

                trainings = trainingQuery
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TrainingItemViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        TrainingDescription = t.TrainingDescription,
                        EmployerAddress = t.Users.EmployerDetails.Address,
                        Price = t.Price,
                        ModeOfPayment = t.ModeOfPayment,
                        PaymentOption = t.PaymentOption,
                        CreatedAt = t.CreatedAt,
                        TrainingBadge = t.TrainingBadge.Badge,
                        AlreadyApplied = false
                    })
                    .AsNoTracking()
                    .ToList();
            }

            // Total for pagination depends on active filter
            int totalItems = filter switch
            {
                "job" => totalJobs,
                "training" => totalTrainings,
                _ => totalJobs + totalTrainings
            };

            var viewModel = new JobListViewModel
            {
                Jobs = jobs,
                Trainings = trainings,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Keyword = keyword,
                Location = location,
                SalaryMin = salaryMin,
                SalaryMax = salaryMax,
                Filter = filter
            };

            return View(viewModel);
        }
        public IActionResult SignIn()
        {
            return View();
        }
        private string BuildOtpEmailTemplate(string otp, string username)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <title>OTP Verification</title>
</head>
<body style=""margin:0;padding:0;background-color:#f5f8fa;font-family:'Inter',Arial,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f5f8fa;padding:40px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);"">

          <!-- Header -->
          <tr>
            <td style=""background:linear-gradient(135deg,#1b84ff 0%,#056ee9 100%);padding:40px 48px;text-align:center;"">
              <h1 style=""color:#ffffff;margin:0;font-size:28px;font-weight:700;letter-spacing:-0.5px;"">Email Verification</h1>
              <p style=""color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:15px;"">Confirm your identity to continue</p>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:48px;"">
              <p style=""color:#4b5675;font-size:15px;margin:0 0 8px;"">Hello, <strong style=""color:#071437;"">{username}</strong></p>
              <p style=""color:#4b5675;font-size:15px;line-height:1.6;margin:0 0 32px;"">
                Use the verification code below to complete your sign-in. This code is valid for <strong>5 minutes</strong>.
              </p>

              <!-- OTP Box -->
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:32px;"">
                <tr>
                  <td align=""center"">
                    <div style=""display:inline-block;background:#f9f9f9;border:2px dashed #1b84ff;border-radius:12px;padding:24px 48px;"">
                      <p style=""margin:0 0 4px;color:#78829d;font-size:12px;font-weight:600;letter-spacing:2px;text-transform:uppercase;"">Your OTP Code</p>
                      <p style=""margin:0;color:#071437;font-size:48px;font-weight:700;letter-spacing:12px;font-family:monospace;"">{otp}</p>
                    </div>
                  </td>
                </tr>
              </table>

              <!-- Warning -->
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#fff8dd;border-left:4px solid #f6c000;border-radius:4px;margin-bottom:32px;"">
                <tr>
                  <td style=""padding:16px 20px;"">
                    <p style=""margin:0;color:#7e6309;font-size:13px;line-height:1.5;"">
                      ⚠️ <strong>Never share this code</strong> with anyone. Our team will never ask for your OTP.
                    </p>
                  </td>
                </tr>
              </table>

              <p style=""color:#78829d;font-size:13px;margin:0;"">
                If you didn't request this code, you can safely ignore this email. Your account remains secure.
              </p>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f9f9f9;border-top:1px solid #eff2f5;padding:24px 48px;text-align:center;"">
              <p style=""color:#99a1b7;font-size:12px;margin:0 0 4px;"">This is an automated message — please do not reply.</p>
              <p style=""color:#99a1b7;font-size:12px;margin:0;"">© {DateTime.Now.Year} Burbodek. All rights reserved.</p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
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

            if (user.EmployerDetails != null)
            {
                if (user.EmployerDetails.Status == "For Approval")
                {
                    TempData["Status"] = "Your account is still pending for admins approval.";
                    return View();
                }
                if (user.EmployerDetails.Status == "Decline" &&( user.EmployerDetails.isAllowedForResubmission == false || user.EmployerDetails.isAllowedForResubmission == null))
                {
                    TempData["Status"] = "Your account is declined: <br><br>Reason for declined: <br><h5>"
                        + user.EmployerDetails.RejectionReason
                        + "</h5><br>Thank you for applying.";
                    return View();
                }
                else if(user.EmployerDetails.Status == "Decline" && (user.EmployerDetails.isAllowedForResubmission == true))
                {
                    TempData["Status"] = "Your account is declined: <br><br>Reason for declined: <br><h5>"
                        + user.EmployerDetails.RejectionReason
                        + "</h5><br>You may re-apply again by signing up the same email.";
                    return View();
                }
            }

            var passwordHasher = new PasswordHasher<Users>();
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);

            if (result != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("Password", "Incorrect password.");
                return View();
            }


            // ✅ Check if user is verified — auto-resend OTP and redirect
            if (user.isVerified == false || user.isVerified == null)
            {
                Random random = new Random();
                int otp = random.Next(100000, 999999);

                user.Otpcode = otp.ToString();
                user.Otpsent = DateTime.Now;
                user.Otpexpiration = DateTime.Now.AddMinutes(5);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                string emailBody = BuildOtpEmailTemplate(otp.ToString(), user.Username);
                await _email.SendEmailAsync(user.Email, "Verify Your Account – OTP Code", emailBody);

                TempData["Success"] = "A new OTP has been sent to your email. Please verify your account.";
                return RedirectToAction("VerifyOtp", new { email = user.Email });
            }

            // ✅ Verified — sign in
            var claims = new List<Claim>
    {
        new Claim("UsersId", user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("Status", user.EmployerDetails?.Status ?? "none"),
        new Claim("isSubscriber", _context.Subscription.Any(s => s.UsersId == user.Id && s.Status == "Current").ToString()),
        new Claim("SubscriberType", _context.Subscription.Where(u => u.Status == "Current" && u.UsersId == user.Id).FirstOrDefault()?.PlansId.ToString() ?? "Expired"),
        new Claim("Plan", _context.Subscription.Include(u => u.Plans).Where(u => u.Status == "Current" && u.UsersId == user.Id).FirstOrDefault()?.Plans.PlanName.ToString() ?? "None"),
        new Claim("isTrainingCenter", _context.EmployerDetails.Any(u => u.UsersId == user.Id && u.isTrainingCenter == 1).ToString()),
        new Claim("isEmployer", _context.EmployerDetails.Any(u => u.UsersId == user.Id && u.isEmployer == 1).ToString()),
        new Claim("Picture", _context.UserProfile.Where(u => u.UsersId == user.Id).FirstOrDefault()?.Picture ?? "/assets/media/avatars/300-14.jpg"),
    };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("MyCookieAuth", principal);

            switch (user.Role)
            {
                case "Admin": return RedirectToAction("Index", "Home");
                case "Client": return RedirectToAction("Index", "Employee");
                case "Employer": return RedirectToAction("Index", "Employer");
                case "Trainer": return RedirectToAction("Index", "Seller");
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> ResendOtp(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("SignIn");
            }

            Random random = new Random();
            int otp = random.Next(100000, 999999);

            user.Otpcode = otp.ToString();
            user.Otpsent = DateTime.Now;
            user.Otpexpiration = DateTime.Now.AddMinutes(5);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();


            string emailBody = BuildOtpEmailTemplate(otp.ToString(), user.Username);

            await _email.SendEmailAsync(user.Email, "OTP Verification", emailBody);

            TempData["Success"] = "OTP has been resent.";
            return RedirectToAction("VerifyOtp", new { email = email });
        }
        public IActionResult SignUpClient() 
        {
            var terms = _context.Terms.FirstOrDefault();
            var signup = new SignUpClientViewModel
            {
                Terms = terms
            };
            return View(signup);
        }
        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(0, 999999).ToString("D6");
        }
        public IActionResult VerifyOtp()
        {
            return View();
        }
        [HttpPost]
        public IActionResult VerifyOtp(string email, string? otp)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("SignIn");
            }

            if (user.Otpcode != otp)
            {
                TempData["Error"] = "Invalid OTP.";
                return View();
            }

            if (user.Otpexpiration < DateTime.Now)
            {
                TempData["Error"] = "OTP expired.";
                return View();
            }

            user.isVerified = true;
            user.Otpcode = null;

            _context.Users.Update(user);
            _context.SaveChanges();

            TempData["Success"] = "Account verified successfully!";
            return RedirectToAction("SignIn");
        }
        [HttpPost]
        public async Task<IActionResult> SignUpClient(SignUpClientViewModel user)
        {
            ModelState.Remove("Users.Role");
            ModelState.Remove("UserProfile.Users");
            ModelState.Remove("Terms");

            if (string.IsNullOrWhiteSpace(user.Users.Username) ||
                string.IsNullOrWhiteSpace(user.Users.Email) ||
                string.IsNullOrWhiteSpace(user.UserProfile.FirstName) ||
                string.IsNullOrWhiteSpace(user.Users.Password) ||
                string.IsNullOrWhiteSpace(user.UserProfile.LastName) ||
                string.IsNullOrWhiteSpace(user.UserProfile.MobileNo) ||
                string.IsNullOrWhiteSpace(user.UserProfile.City))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == user.Users.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(user);
                }

                user.Users.Role = "Client";
                user.Users.DateCreated = DateTime.Now;

                var passwordHasher = new PasswordHasher<Users>();
                user.Users.Password = passwordHasher.HashPassword(user.Users, user.Users.Password);

                // ✅ Generate OTP
                string otp = GenerateOtp();

                user.Users.Otpcode = otp;
                user.Users.Otpsent = DateTime.Now;
                user.Users.Otpexpiration = DateTime.Now.AddMinutes(5); // OTP valid for 5 minutes
                user.Users.isVerified = false;

                _context.Users.Add(user.Users);
                await _context.SaveChangesAsync();

                // Save profile
                user.UserProfile.UsersId = user.Users.Id;
                _context.UserProfile.Add(user.UserProfile);
                await _context.SaveChangesAsync();

                // Send OTP Email
                string emailBody = BuildOtpEmailTemplate(otp, user.Users.Username);

                await _email.SendEmailAsync(user.Users.Email, "Verify Your Account", emailBody);

                TempData["Success"] = "Account created. Please verify your email using the OTP sent.";
                return RedirectToAction("VerifyOtp", new { email = user.Users.Email });
            }

            ModelState.AddModelError("", "All fields are required.");
            return View(user);
        }
        public IActionResult SignUpEmployer()
        {
            var data = _context.Terms.FirstOrDefault();
            return View(data);
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

            var data = _context.Terms.FirstOrDefault();
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
                if (_context.Users.Any(u => u.Email == employer.Users.Email && u.EmployerDetails.isAllowedForResubmission == true && u.EmployerDetails.RegistrationCount > 3))
                {
                    TempData["Error"] = "This email is permanently banned.";
                    return View(data);
                }else if (_context.Users.Any(u => u.Email == employer.Users.Email && u.EmployerDetails.isAllowedForResubmission != true))
                {
                    TempData["Error"] = "This email is already used.";
                    return View(data);
                }
                else if(_context.Users.Any(u => u.Email == employer.Users.Email && u.EmployerDetails.isAllowedForResubmission == true && u.EmployerDetails.RegistrationCount <= 3))
                {
                    var existingUser = _context.Users
                        .Include(u => u.EmployerDetails)
                        .FirstOrDefault(u => u.Email == employer.Users.Email);
                    if (existingUser != null)
                    {
                        existingUser.Username = employer.Users.Username;
                        existingUser.Email = employer.Users.Email;
                        existingUser.Password = new PasswordHasher<Users>().HashPassword(existingUser, employer.Users.Password);
                        existingUser.EmployerDetails.RegistrationCount += 1;
                        existingUser.EmployerDetails.isAllowedForResubmission = existingUser.EmployerDetails.RegistrationCount > 3 ? false : true;
                        existingUser.EmployerDetails.BusinessName = employer.BusinessName;
                        existingUser.EmployerDetails.BusinessDescription = employer.BusinessDescription;
                        existingUser.EmployerDetails.Address = employer.Address;
                        existingUser.EmployerDetails.Latitude = employer.Latitude;
                        existingUser.EmployerDetails.Longitude = employer.Longitude;
                        existingUser.EmployerDetails.isEmployer = employer.isEmployer ?? 0;
                        existingUser.EmployerDetails.isTrainingCenter = employer.isTrainingCenter ?? 0;
                        existingUser.EmployerDetails.Status = "For Approval";
                        void ReuploadFile(IFormFile? file, string details)
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
                                var existingFile = _context.Files.FirstOrDefault(f => f.UsersId == existingUser.Id && f.ImageDetails == details);
                                if (existingFile != null)
                                {
                                    existingFile.FileName = file.FileName;
                                    existingFile.ContentType = file.ContentType;
                                    existingFile.File = $"/uploads/{uniqueFileName}";
                                    _context.Files.Update(existingFile);
                                }
                                else
                                {
                                    var newFile = new Files
                                    {
                                        UsersId = existingUser.Id,
                                        ImageDetails = details,
                                        FileName = file.FileName, // original name
                                        ContentType = file.ContentType,
                                        File = $"/uploads/{uniqueFileName}" // relative path for serving
                                    };
                                    _context.Files.Add(newFile);
                                }

                                _context.SaveChanges();
                            }
                        }

                        ReuploadFile(sec_dti, "sec_dti");
                        ReuploadFile(bir_certificate, "bir_certificate");
                        ReuploadFile(business_permit, "business_permit");
                        ReuploadFile(poea_license, "poea_license");
                        ReuploadFile(proof_partnership, "proof_partnership");

                        _context.Users.Update(existingUser);
                        _context.SaveChanges();
                        TempData["Success"] = "Email successfully registered!";
                        return RedirectToAction("SignIn", "Index");
                    }
                }

                    // ✅ Create user
                    var user = new Users
                    {
                        Username = employer.Users.Username,
                        Email = employer.Users.Email,
                        Password = employer.Users.Password,
                        Role = "Employer",
                        DateCreated = DateTime.Now,
                        isVerified = true
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
                    Longitude = employer.Longitude,
                    RegistrationCount = 1
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
            return View(data);
        }

    }
}
