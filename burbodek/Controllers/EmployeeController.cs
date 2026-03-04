using burbodek.Data;
using burbodek.Models;
using burbodek.Models.DTO;
using burbodek.Models.ViewModels;
using burbodek.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Threading;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Client")]
    public class EmployeeController : Controller
    {
        ApplicationDbContext _context;
        private readonly IPaymongo _paymongo;
        private readonly IWebHostEnvironment _environment;
        public EmployeeController(ApplicationDbContext context,IPaymongo paymongo, IWebHostEnvironment environment)
        {
            _context = context;
            _paymongo = paymongo;
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profileImage)
        {
            try
            {
                if (profileImage == null || profileImage.Length == 0)
                    return Json(new { success = false, message = "No image provided." });

                // Validate extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(profileImage.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Invalid file type. Only JPG, PNG, and WEBP are allowed." });

                // Validate size (5MB)
                if (profileImage.Length > 5 * 1024 * 1024)
                    return Json(new { success = false, message = "File size must not exceed 5MB." });

                // Get current user
                var userId = int.Parse(User.FindFirst("UsersId")?.Value);
                var user = await _context.Users.Include(u=> u.UserProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                if(user.UserProfile != null)
                {
                    // Delete old profile picture if it's not the default
                    if (!string.IsNullOrEmpty(user.UserProfile.Picture))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                            user.UserProfile.Picture.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    // Save new file
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    // Update DB
                    var relativePath = $"/uploads/profiles/{fileName}";
                    user.UserProfile.Picture = relativePath;

                    await _context.SaveChangesAsync();
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
                    new Claim("Picture", _context.UserProfile.Where(u => u.UsersId == user.Id).FirstOrDefault()?.Picture ?? "/assets/media/avatars/300-14.jpg"),
                };

                    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync("MyCookieAuth", principal);
                    return Json(new { success = true, path = relativePath });
                }
                else
                {
                    // Save new file
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    // Update DB
                    var relativePath = $"/uploads/profiles/{fileName}";
                    var data = new UserProfile
                    {
                        Picture = relativePath,
                        UsersId = userId
                    };
                    await _context.UserProfile.AddAsync(data);
                    await _context.SaveChangesAsync();
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
                    new Claim("Picture", _context.UserProfile.Where(u => u.UsersId == user.Id).FirstOrDefault()?.Picture ?? "/assets/media/avatars/300-14.jpg"),
                };

                    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync("MyCookieAuth", principal);
                    return Json(new { success = true, path = relativePath });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult Index(string keyword, string location, decimal? salaryMin, decimal? salaryMax, int page = 1)
        {
            int pageSize = 10;
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            // --- JOBS QUERY ---
            var jobQuery = _context.Jobs
                .Include(j => j.Users)
                    .ThenInclude(u => u.EmployerDetails)
                .Include(j => j.JobApplication)
                .Where(j => j.ExpirationDate > DateTime.Now && j.isArchived == null && j.isDeleted == null);

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

            // ✅ Salary Range Filter
            if (salaryMin.HasValue)
            {
                jobQuery = jobQuery.Where(j => j.SalaryMax >= salaryMin.Value);
            }

            if (salaryMax.HasValue)
            {
                jobQuery = jobQuery.Where(j => j.SalaryMin <= salaryMax.Value);
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
                    CreatedAt = j.CreatedAt,
                    JobRequiredBadge = j.JobRequiredBadge.ToList(),
                    AlreadyApplied = j.JobApplication.Any(a => a.AppliedBy == userId)
                })
                .AsNoTracking()
                .ToList();

            // --- TRAININGS QUERY ---
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
                    CreatedAt = t.CreatedAt,
                    TrainingBadge = t.TrainingBadge.Badge,
                    AlreadyApplied = t.TrainingApplication.Any(a => a.AppliedBy == userId)
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
                Location = location,
                SalaryMin = salaryMin,
                SalaryMax = salaryMax
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> TrashedView(int id, int RecipientId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);


                var draftCount = _context.EmailThreads
                    .Include(t => t.Emails)
                    .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                    .Count();
                var inboxCount = _context.EmailRecipients
                    .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                    .Count();
                ViewBag.DraftCount = draftCount;
                ViewBag.InboxCount = inboxCount;
                // Get the thread with all emails

                var thread = await _context.EmailThreads
                    .Include(t => t.Emails)
                        .ThenInclude(e => e.Sender)
                    .Include(t => t.Emails)
                        .ThenInclude(e => e.Recipients)
                            .ThenInclude(r => r.Recipient)
                    .Include(t => t.Emails)
                        .ThenInclude(e => e.Attachments)
                    .FirstOrDefaultAsync(t =>
                        t.Id == id &&
                        t.Emails.Any()
                    );
                if (thread == null)
                {
                    TempData["Error"] = "Thread not found";
                    return RedirectToAction("Index");
                }

                // Get the latest email in the thread
                var latestEmail = thread.Emails.OrderByDescending(e => e.SentAt).FirstOrDefault();

                if (latestEmail == null)
                {
                    TempData["Error"] = "No emails found in thread";
                    return RedirectToAction("Index");
                }

                // Prepare reply data
                var toRecipients = new List<dynamic>();
                var ccRecipients = new List<dynamic>();

                // Reply-to logic: Add original sender and all TO recipients (excluding current user)
                if (latestEmail.SenderID != currentUserId)
                {
                    toRecipients.Add(new
                    {
                        email = latestEmail.Sender.Email,
                        fullName = latestEmail.Sender.Username
                    });
                }

                foreach (var recipient in latestEmail.Recipients.Where(r => r.RecipientType == RecipientType.TO && r.RecipientID != currentUserId))
                {
                    toRecipients.Add(new
                    {
                        email = recipient.Recipient.Email,
                        fullName = recipient.Recipient.Username
                    });
                }

                // Include CC recipients (excluding current user)
                foreach (var recipient in latestEmail.Recipients.Where(r => r.RecipientType == RecipientType.CC && r.RecipientID != currentUserId))
                {
                    ccRecipients.Add(new
                    {
                        email = recipient.Recipient.Email,
                        fullName = recipient.Recipient.Username
                    });
                }

                // Prepare view model
                var viewModel = new ThreadViewModel
                {
                    ThreadID = thread.Id,
                    Subject = thread.Subject,
                    Emails = thread.Emails.OrderBy(e => e.SentAt).ToList()
                };

                // Prepare reply data for ViewBag
                ViewBag.ReplyData = new
                {
                    ToRecipients = toRecipients,
                    CcRecipients = ccRecipients,
                    BccRecipients = new List<dynamic>(),
                    ReplySubject = thread.Subject.StartsWith("Re: ") ? thread.Subject : $"Re: {thread.Subject}",
                    OriginalBody = latestEmail.Body,
                    OriginalSenderName = latestEmail.Sender.Username,
                    OriginalSentAt = latestEmail.SentAt.ToString("MMM dd, yyyy 'at' hh:mm tt")
                };
                ViewBag.Id = RecipientId;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                return RedirectToAction("TrashedEmail");
            }
        }
        public IActionResult TrashedRestore(int Id)
        {
            if (Id == null || Id == 0)
                return Json(new { success = false, message = "No emails to restore." });

            var currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

            try
            {
                var recipients = _context.EmailRecipients
                    .Include(er => er.Email)
                        .ThenInclude(e => e.Thread)
                    .Where(er => er.Id == Id)
                    .ToList();


                foreach (var recipient in recipients)
                {
                    if (recipient.Email.Thread.CreatedBy == currentUserId)
                    {
                        recipient.Email.Thread.IsTrashed = false;
                    }
                    else
                    {
                        recipient.IsTrashed = false;
                    }
                }

                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error." });
            }
        }
        public IActionResult MarkAsStarred(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            if (Id != null)
            {
                var email = _context.EmailRecipients.FirstOrDefault(u => u.Email.ThreadID == Id && u.RecipientID == userId);
                if (email.IsStarred)
                {
                    email.IsStarred = false;
                }
                else
                {
                    email.IsStarred = true;
                }
                _context.SaveChanges();
            }

            return Json(new { response = Id });
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(decimal amountPaid, string amountPaidIn, string email, string username, int Id)
        {
            try
            {
                // ✅ Basic validation
                if (amountPaid <= 0 || string.IsNullOrWhiteSpace(amountPaidIn) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(username))
                {
                    // Return back with validation error
                    TempData["Error"] = "Invalid checkout details. Please make sure all fields are filled correctly.";
                    return RedirectToAction("TrainingPayment");
                }

                // ✅ Create checkout session
                var responseJson = await _paymongo.TrainingCheckout(
                    amountPaid,
                    "PHP",
                    username,
                    email,
                    amountPaidIn
                );

                // ✅ Parse response
                var json = JObject.Parse(responseJson);
                var checkoutUrl = json["data"]?["attributes"]?["checkout_url"]?.ToString();

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    TempData["Error"] = "Failed to retrieve checkout URL. Please try again later.";
                    return RedirectToAction("Subscription", "Employer");
                }

                TempData["PlanName"] = amountPaidIn;
                TempData["Amount"] = (int)amountPaid;
                TempData["Id"] = Id;
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Checkout failed: {ex.Message}";
                return RedirectToAction("TrainingPayment", "Employee");
            }
        }
        public IActionResult SuccessPayment()
        {
            var planName = TempData["PlanName"]?.ToString();
            int amount = TempData["Amount"] != null ? Convert.ToInt32(TempData["Amount"]) : 0;
            int TrainingPaymentId = TempData["Id"] != null ? Convert.ToInt32(TempData["Id"]) : 0;

            var paymentDetails = _context.TrainingPayments.Find(TrainingPaymentId);
            if (paymentDetails != null)
            {
                paymentDetails.Paid = amount;
                _context.TrainingPayments.Update(paymentDetails);
                _context.SaveChanges();
            }
            TempData["success"] = "Payment Successful!";
            return View();
        }
        public IActionResult TrainingPayment(int Id)
        {
            var usersId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.TrainingPayments
                                .Include(u => u.TrainingApplication)
                                    .ThenInclude(u => u.Training)
                                        .ThenInclude(u => u.Users)
                                .Where(u => u.Id == Id).FirstOrDefault();
            var userInfo = _context.Users.Where(u => u.Id == usersId).FirstOrDefault();

            var payment = new TrainingPaymentViewModel
            {
                TrainingPayments = data,
                Users = userInfo
            };
            TempData["Id"] = Id;
            return View(payment);
        }
        public IActionResult Training()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var training = _context.TrainingApplication
                .Where(a => a.AppliedBy == userId && ((a.TrainingPayments.FirstOrDefault().ModeOfPayment == "E-wallet" && a.TrainingPayments.FirstOrDefault().Paid != null)||(a.TrainingPayments.FirstOrDefault().ModeOfPayment == "Cash")))
                .Include(a => a.Training)
                    .ThenInclude(u => u.Users)
                .Include(a=> a.TrainingPayments)
                    .ThenInclude(a => a.Users)
                .ToList();

            return View(training);
        }
        public IActionResult Dashboard()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.TrainingPayments
                                .Include(u => u.TrainingApplication)
                                    .ThenInclude(u => u.Training)
                                        .ThenInclude(u => u.Users)
                                .Where(u => u.UsersId == userId && u.TrainingApplication.Training.Expiration >= DateTime.Now && u.ModeOfPayment == "E-wallet" && u.Paid == null).ToList();
            var campaign = _context.Campaign.Where(u => u.IsActive).ToList();
            var dashboard = new EmployeeDashboardViewModel
            {
                Training = data,
                Campaign = campaign
            };
            return View(dashboard);
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
                        .Include(u => u.JobRequiredBadge)
                        .Include(u => u.JobApplication.Where(ap => ap.AppliedBy == userId))
                        .Where(u => u.Id == Id && u.isArchived == null)
                        .FirstOrDefault();
            var requiredBadges = data.JobRequiredBadge
                                .Select(b => b.Badge)
                                .ToList();
            var trainings = _context.Training
                        .Include(t => t.TrainingBadge)
                        .Include(t => t.TrainingApplication)
                        .Include(t => t.Users)
                        .Include(t => t.TrainingMedia)
                        .Where(t =>
                            requiredBadges.Contains(t.TrainingBadge.Badge) &&
                            !t.TrainingApplication.Any(a => a.AppliedBy == userId)
                        )
                        .OrderBy(t => t.Expiration) // optional: prioritize soon-expiring
                        .Take(3)
                        .ToList();
            var badge = _context.UserBadge.Where(u => u.UsersId == userId).ToList();
            ViewBag.UserBadges = badge;
            var jobInfo = new JobInfoViewModel
            {
                Jobs = data,
                Training = trainings
            };
            return View(jobInfo);
        }
        public IActionResult TrainingViewCampaign(int Id, int CampaignId)
        {
            var data = _context.Campaign.Find(CampaignId);
            if (data == null)
            {
                // You can redirect to an error page or show a message
                return NotFound("Campaign not found");
            }

            data.TotalClicks += 1;
            _context.Campaign.Update(data);
            _context.SaveChanges();

            return RedirectToAction("TrainingInfo", new { Id = Id });
        }
        public IActionResult JobViewCampaign(int Id, int CampaignId)
        {
            var data = _context.Campaign.Find(CampaignId);
            if (data == null)
            {
                // You can redirect to an error page or show a message
                return NotFound("Campaign not found");
            }

            data.TotalClicks += 1;
            _context.Campaign.Update(data);
            _context.SaveChanges();

            return RedirectToAction("JobInfo", new { Id = Id });
        }
        public IActionResult TrainingInfo(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")!.Value);
            var training = _context.Training
                .Include(u => u.Users)
                    .ThenInclude(u => u.EmployerDetails)
                .Include(u => u.TrainingRequirements)
                .Include(u => u.TrainingMedia)
                .Include(u => u.TrainingBadge)
                .Include(u => u.TrainingBenefits)
                .Include(u => u.TrainingApplication.Where(ap => ap.AppliedBy == userId))
                .Include(u => u.TrainingUploads.Where(ap => ap.isActive))
                .Where(u => u.Id == Id && u.isArchived == null)
                .FirstOrDefault();

            if (training == null)
                return NotFound();

            // 🔹 Badge obtained from this training
            var trainingBadge = training.TrainingBadge?.Badge;

            // 🔹 User's existing badges
            var userBadges = _context.UserBadge
                .Where(ub => ub.UsersId == userId)
                .Select(ub => ub.Badge)
                .ToList();

            // 🔹 Combine badges: existing + training badge
            var effectiveBadges = userBadges
                .Append(trainingBadge)
                .ToList();

            // 🔥 RECOMMENDED JOBS
            var recommendedJobs = _context.Jobs
                .Include(j => j.JobRequiredBadge)
                .Include(j => j.JobApplication)
                .Include(j => j.Users)
                    .ThenInclude(j => j.EmployerDetails)
                .Include(j => j.JobMedia)
                .Where(j =>
                    // ❌ Not yet applied
                    !j.JobApplication.Any(a => a.AppliedBy == userId)

                    // ✅ ALL required badges must be satisfied
                    && j.JobRequiredBadge.All(rb =>
                        effectiveBadges.Contains(rb.Badge)
                    )

                    // ✅ Training must actually contribute
                    && j.JobRequiredBadge.Any(rb =>
                        rb.Badge == trainingBadge
                    )
                )
                .OrderBy(j => j.ExpirationDate)
                .Take(3)
                .ToList();
            var trainingInfo = new TrainingInfoViewModel
            {
                Training = training,
                Jobs = recommendedJobs
            };
            return View(trainingInfo);
        }

        public IActionResult TrashedEmail()
        {
            int currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

            var trashedEmails = _context.Emails
                .Include(e => e.Thread)
                    .ThenInclude(e => e.Creator)
                .Include(e => e.Thread)
                    .ThenInclude(e => e.Emails)
                        .ThenInclude(e => e.Recipients)
                .Include(e => e.Thread)
                    .ThenInclude(e => e.Emails)
                        .ThenInclude(e => e.Sender)
                .Include(e => e.Sender)
                .Include(e => e.Recipients)
                .Where(e =>
                    (e.SenderID == currentUserId && e.IsTrashed) ||
                    e.Thread.CreatedBy == currentUserId && e.Thread.IsTrashed ||
                    e.Recipients.Any(r => r.RecipientID == currentUserId && r.IsTrashed)
                )
                .OrderByDescending(r => r.SentAt)
                .Select(u => u.Thread)
                .Distinct()
                .ToList();
            var draftCount = _context.EmailThreads
                .Include(t => t.Emails)
                .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                .Count();
            var inboxCount = _context.EmailRecipients
                .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                .Count();
            ViewBag.DraftCount = draftCount;
            ViewBag.InboxCount = inboxCount;
            return View(trashedEmails);
        }
        public IActionResult EmailSent()
        {
            int currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

            var inboxEmails = _context.EmailRecipients
                .Include(r => r.Email)
                    .ThenInclude(e => e.Recipients)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Thread)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Sender)
                .Where(e => e.Email.SenderID == currentUserId && !e.Email.IsTrashed && !e.Email.Thread.IsTrashed)
                .OrderByDescending(r => r.Email.SentAt)
                .Select(r => r.Email)
                .Distinct()
                .ToList();
            var draftCount = _context.EmailThreads
                .Include(t => t.Emails)
                .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                .Count();
            var inboxCount = _context.EmailRecipients
                .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                .Count();
            ViewBag.DraftCount = draftCount;
            ViewBag.InboxCount = inboxCount;
            return View(inboxEmails);
        }
        public IActionResult MarkedEmail()
        {
            int currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

            var inboxEmails = _context.EmailRecipients
                .Include(r => r.Email)
                    .ThenInclude(e => e.Recipients)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Thread)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Sender)
                .Where(r => !r.IsTrashed && r.IsStarred && r.RecipientID == currentUserId)
                .OrderByDescending(r => r.Email.SentAt)
                .Select(r => r.Email)
                .Distinct()
                .ToList();
            var draftCount = _context.EmailThreads
                .Include(t => t.Emails)
                .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                .Count();
            var inboxCount = _context.EmailRecipients
                .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                .Count();
            ViewBag.DraftCount = draftCount;
            ViewBag.InboxCount = inboxCount;
            return View(inboxEmails);
        }
        public IActionResult DraftEmail()
        {
            int currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

            var inboxEmails = _context.EmailRecipients
                .Include(r => r.Email)
                    .ThenInclude(e => e.Recipients)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Thread)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Sender)
                .Where(r => !r.IsTrashed && r.Email.IsDraft && r.Email.SenderID == currentUserId && !r.Email.Thread.IsTrashed)
                .OrderByDescending(r => r.Email.SentAt)
                .Select(r => r.Email)
                .Distinct()
                .ToList();
            var draftCount = _context.EmailThreads
                .Include(t => t.Emails)
                .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                .Count();
            var inboxCount = _context.EmailRecipients
                .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                .Count();
            ViewBag.DraftCount = draftCount;
            ViewBag.InboxCount = inboxCount;
            return View(inboxEmails);
        }
        public IActionResult Message()
        {
            int currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

            var inboxEmails = _context.EmailRecipients
                .Include(r => r.Email)
                    .ThenInclude(e => e.Thread)
                        .ThenInclude(t => t.Creator)
                .Include(r => r.Email.Sender)
                .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.Email.IsTrashed && !r.Email.Thread.IsTrashed)
                .OrderByDescending(r => r.Email.SentAt)
                .AsEnumerable()
                .GroupBy(r => r.Email.ThreadID)
                .Select(g => new InboxViewModel
                {
                    Thread = g.First().Email.Thread,
                    Email = g.OrderByDescending(x => x.Email.SentAt).First().Email, // ✅ FIXED
                    Recipient = g.First(x => x.RecipientID == currentUserId)
                })
                .ToList();
            var draftCount = _context.EmailThreads
                .Include(t => t.Emails)
                .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                .Count();
            var inboxCount = _context.EmailRecipients
                .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                .Count();

            ViewBag.DraftCount = draftCount;
            ViewBag.InboxCount = inboxCount;

            return View(inboxEmails);
        }

        public IActionResult Compose()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Reply(int id)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

                // Mark all messages in this thread as read for the current user
                var changeIsRead = _context.EmailRecipients
                    .Where(u => u.Email.ThreadID == id && u.RecipientID == currentUserId)
                    .ToList();

                if (changeIsRead.Any())
                {
                    foreach (var data in changeIsRead)
                        data.IsRead = true;

                    _context.SaveChanges();
                }

                var draftCount = _context.EmailThreads
                    .Include(t => t.Emails)
                    .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                    .Count();
                var inboxCount = _context.EmailRecipients
                    .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                    .Count();
                ViewBag.DraftCount = draftCount;
                ViewBag.InboxCount = inboxCount;

                // Load the thread and all its related data
                var thread = await _context.EmailThreads
                    .Include(t => t.Emails)
                        .ThenInclude(e => e.Sender)
                    .Include(t => t.Emails)
                        .ThenInclude(e => e.Recipients)
                            .ThenInclude(r => r.Recipient)
                    .Include(t => t.Emails)
                        .ThenInclude(e => e.Attachments)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (thread == null)
                {
                    TempData["Error"] = "Thread not found.";
                    return RedirectToAction("Index");
                }

                // 🧠 Filter only the emails the current user should see
               var visibleEmails = thread.Emails
                .Where(e =>
                    e.SenderID == currentUserId ||
                    e.Recipients.Any(r => r.RecipientID == currentUserId))
                .OrderBy(e => e.SentAt)
                .ToList();


                if (!thread.Emails.Any())
                {
                    TempData["Error"] = "You do not have permission to view this thread.";
                    return RedirectToAction("Index");
                }

                // Get latest visible email
                var latestEmail = thread.Emails.OrderByDescending(e => e.SentAt).FirstOrDefault();
                if (latestEmail == null)
                {
                    TempData["Error"] = "No emails found in this thread.";
                    return RedirectToAction("Index");
                }

                // Prepare recipient data for reply
                var toRecipients = new List<dynamic>();
                var ccRecipients = new List<dynamic>();

                // Add the sender (if not current user)
                if (latestEmail.SenderID != currentUserId)
                {
                    toRecipients.Add(new
                    {
                        email = latestEmail.Sender.Email,
                        fullName = latestEmail.Sender.Username
                    });
                }

                // Add TO recipients except current user
                foreach (var recipient in latestEmail.Recipients
                    .Where(r => r.RecipientType == RecipientType.TO && r.RecipientID != currentUserId))
                {
                    toRecipients.Add(new
                    {
                        email = recipient.Recipient.Email,
                        fullName = recipient.Recipient.Username
                    });
                }

                // Add CC recipients except current user
                foreach (var recipient in latestEmail.Recipients
                    .Where(r => r.RecipientType == RecipientType.CC && r.RecipientID != currentUserId))
                {
                    ccRecipients.Add(new
                    {
                        email = recipient.Recipient.Email,
                        fullName = recipient.Recipient.Username
                    });
                }

                // Prepare ViewModel
                var viewModel = new ThreadViewModel
                {
                    ThreadID = thread.Id,
                    Subject = thread.Subject,
                    Emails = visibleEmails
                };

                // Prepare ReplyData for the view
                ViewBag.ReplyData = new
                {
                    ToRecipients = toRecipients,
                    CcRecipients = ccRecipients,
                    BccRecipients = new List<dynamic>(),
                    ReplySubject = thread.Subject.StartsWith("Re: ", StringComparison.OrdinalIgnoreCase)
                        ? thread.Subject
                        : $"Re: {thread.Subject}",
                    OriginalBody = latestEmail.Body,
                    OriginalSenderName = latestEmail.Sender.Username,
                    OriginalSentAt = latestEmail.SentAt.ToString("MMM dd, yyyy 'at' hh:mm tt")
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading reply: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DraftEdit(int id)
        {
            try
            {
                int currentUserId = int.Parse(User.FindFirst("UsersId")?.Value); // however you identify logged-in user
                var inboxEmails = _context.Emails
                        .Include(e => e.Recipients)
                            .ThenInclude(e => e.Recipient)
                        .Include(e => e.Thread)
                        .Include(e => e.Sender)
                        .Include(e => e.Attachments)
                    .Where(r => r.Id == id && r.IsDraft)
                    .FirstOrDefault();

                var draftCount = _context.EmailThreads
                    .Include(t => t.Emails)
                    .Where(t => t.Emails.Any(e => e.IsDraft && !e.Thread.IsTrashed && e.SenderID == currentUserId))
                    .Count();
                var inboxCount = _context.EmailRecipients
                    .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.Thread.IsTrashed)
                    .Count();
                ViewBag.DraftCount = draftCount;
                ViewBag.InboxCount = inboxCount;
                return View(inboxEmails);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading reply: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ReplyEmail(ReplyEmailViewModel model, List<IFormFile> files)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

                if (currentUserId == 0)
                {
                    TempData["Error"] = "User not authenticated";
                    return RedirectToAction("Index");
                }

                // Get the existing thread
                var thread = await _context.EmailThreads
                    .FirstOrDefaultAsync(t => t.Id == model.Id);

                if (thread == null)
                {
                    TempData["Error"] = "Thread not found";
                    return RedirectToAction("Index");
                }

                // Parse recipients
                var toRecipients = ParseRecipients(model.ToRecipients);
                var ccRecipients = ParseRecipients(model.CcRecipients);
                var bccRecipients = ParseRecipients(model.BccRecipients);

                // Validate
                if (!toRecipients.Any())
                {
                    TempData["Error"] = "Please add at least one recipient";
                    return RedirectToAction("Reply", new { id = model.Id });
                }

                // Get recipient user IDs from database
                var allEmails = toRecipients.Concat(ccRecipients).Concat(bccRecipients).Distinct().ToList();
                var recipients = await _context.Users
                    .Where(u => allEmails.Contains(u.Email))
                    .ToListAsync();

                if (recipients.Count != allEmails.Count)
                {
                    TempData["Error"] = "Some recipients were not found";
                    return RedirectToAction("Reply", new { id = model.Id });
                }

                // Update thread subject if changed
                if (!string.IsNullOrWhiteSpace(model.Subject) && model.Subject != thread.Subject)
                {
                    thread.Subject = model.Subject;
                    _context.EmailThreads.Update(thread);
                }

                // Create Email (reply in the same thread)
                var email = new Email
                {
                    ThreadID = thread.Id,
                    SenderID = currentUserId,
                    Body = model.Body,
                    SentAt = DateTime.Now,
                    IsDraft = false,
                    IsTrashed = false,
                    IsRead = true, // Sender has read it
                    IsStarred = false
                };
                _context.Emails.Add(email);
                await _context.SaveChangesAsync();

                // Add Recipients
                var emailRecipients = new List<EmailRecipient>();

                // TO recipients
                foreach (var toEmail in toRecipients)
                {
                    var user = recipients.First(r => r.Email == toEmail);
                    emailRecipients.Add(new EmailRecipient
                    {
                        EmailID = email.Id,
                        RecipientID = user.Id,
                        RecipientType = RecipientType.TO,
                        IsRead = false,
                        IsTrashed = false,
                        IsStarred = false
                    });
                }

                // CC recipients
                foreach (var ccEmail in ccRecipients)
                {
                    var user = recipients.First(r => r.Email == ccEmail);
                    emailRecipients.Add(new EmailRecipient
                    {
                        EmailID = email.Id,
                        RecipientID = user.Id,
                        RecipientType = RecipientType.CC,
                        IsRead = false,
                        IsTrashed = false,
                        IsStarred = false
                    });
                }

                // BCC recipients
                foreach (var bccEmail in bccRecipients)
                {
                    var user = recipients.First(r => r.Email == bccEmail);
                    emailRecipients.Add(new EmailRecipient
                    {
                        EmailID = email.Id,
                        RecipientID = user.Id,
                        RecipientType = RecipientType.BCC,
                        IsRead = false,
                        IsTrashed = false,
                        IsStarred = false
                    });
                }

                _context.EmailRecipients.AddRange(emailRecipients);
                await _context.SaveChangesAsync();

                // Handle Attachments
                if (files != null && files.Any())
                {
                    var attachments = await SaveAttachments(files, email.Id);
                    _context.EmailAttachments.AddRange(attachments);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Reply sent successfully!";
                return RedirectToAction("Reply", new { id = thread.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error sending reply: {ex.Message}";
                return RedirectToAction("Reply", new { id = model.Id });
            }
        }
        // Helper: Add recipients from comma-separated emails
        private async Task AddRecipients(Email email, string emailsCsv, RecipientType type)
        {
            if (string.IsNullOrWhiteSpace(emailsCsv)) return;

            var emails = emailsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(e => e.Trim().ToLower())
                                  .Distinct();

            foreach (var emailStr in emails)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailStr);
                if (user == null) continue; // or create pending recipient?

                email.Recipients.Add(new EmailRecipient
                {
                    RecipientID = user.Id,
                    EmailID = email.Id,
                    RecipientType = type
                });
            }
        }

        // Helper: Build model for view (used on validation fail)
        private async Task<Email> BuildEmailModel(int emailId)
        {
            return await _context.Emails
                .Include(e => e.Thread)
                .Include(e => e.Recipients).ThenInclude(r => r.Recipient)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == emailId);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDraft(ComposeEmailDto dto)
        {
            if (!ModelState.IsValid)
                return View("DraftEdit", await BuildEmailModel(dto.EmailId ?? 0));

            var email = await _context.Emails
                .Include(e => e.Thread)
                .Include(e => e.Recipients)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == dto.EmailId);

            if (email == null)
                return NotFound();

            email.Thread.Subject = dto.Subject;
            email.Body = dto.Body;
            email.IsDraft = false;

            _context.EmailRecipients.RemoveRange(email.Recipients);
            email.Recipients.Clear();

            await AddRecipientsSafe(email, dto.ToRecipients, RecipientType.TO);
            await AddRecipientsSafe(email, dto.CcRecipients, RecipientType.CC);
            await AddRecipientsSafe(email, dto.BccRecipients, RecipientType.BCC);

            if (dto.RemovedAttachmentIds?.Length > 0)
            {
                var toRemove = email.Attachments
                    .Where(a => dto.RemovedAttachmentIds.Contains(a.Id))
                    .ToList();

                foreach (var att in toRemove)
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, att.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);

                    email.Attachments.Remove(att);
                    _context.EmailAttachments.Remove(att);
                }
            }

            // Add new files
            if (dto.Files?.Length > 0)
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "email-attachments", email.Id.ToString());
                Directory.CreateDirectory(uploadPath);

                foreach (var file in dto.Files)
                {
                    if (file.Length == 0) continue;

                    var fileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);
                    var relativePath = $"/uploads/email-attachments/{email.Id}/{fileName}";

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    email.Attachments.Add(new EmailAttachment
                    {
                        FileName = file.FileName,
                        FileSize = file.Length,
                        FilePath = relativePath
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Email sent!";
            return RedirectToAction("EmailSent"); // or "Sent"
        }
        private async Task AddRecipientsSafe(Email email, string emailsCsv, RecipientType type)
        {
            if (string.IsNullOrWhiteSpace(emailsCsv)) return;

            var emails = emailsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLower())
                .Distinct()
                .ToList();

            foreach (var addr in emails)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == addr);

                if (user == null) continue;

                if (user == null)
                {
                    user = new Users
                    {
                        Email = addr,
                        Username = addr.Split('@')[0],   // simple name
                                                         // set any other required fields (e.g. IsActive = false, etc.)
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();   // get generated Id
                }

                email.Recipients.Add(new EmailRecipient
                {
                    RecipientID = user.Id,
                    EmailID = email.Id,
                    RecipientType = type
                });
            }
        }

        // POST: Save as draft (update only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DraftSaveDraft(ComposeEmailDto dto)
        {
            if (!dto.EmailId.HasValue)
                return BadRequest("Draft ID required.");

            var email = await _context.Emails
                .Include(e => e.Thread)
                .Include(e => e.Recipients)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == dto.EmailId && e.IsDraft);

            if (email == null) return NotFound();

            email.Thread.Subject = dto.Subject;
            email.Body = dto.Body;

            _context.EmailRecipients.RemoveRange(email.Recipients);
            email.Recipients.Clear();

            await AddRecipients(email, dto.ToRecipients, RecipientType.TO);
            await AddRecipients(email, dto.CcRecipients, RecipientType.CC);
            await AddRecipients(email, dto.BccRecipients, RecipientType.BCC);

            // Handle removed + new attachments (same as SendEmail)
            if (dto.RemovedAttachmentIds?.Length > 0)
            {
                var toRemove = email.Attachments.Where(a => dto.RemovedAttachmentIds.Contains(a.Id)).ToList();
                foreach (var att in toRemove)
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, att.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);

                    email.Attachments.Remove(att);
                    _context.EmailAttachments.Remove(att);
                }
            }

            if (dto.Files?.Length > 0)
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "emails", email.Id.ToString());
                Directory.CreateDirectory(uploadPath);

                foreach (var file in dto.Files)
                {
                    if (file.Length == 0) continue;

                    var fileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);
                    var relativePath = "/uploads/emails/" + email.Id + "/" + fileName;

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    email.Attachments.Add(new EmailAttachment
                    {
                        FileName = file.FileName,
                        FileSize = file.Length,
                        FilePath = relativePath
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Email saved to draft";
            return RedirectToAction("EmailSent");
        }
        [HttpPost]
        public async Task<IActionResult> SendEmail(SendEmailViewModel model, List<IFormFile> files)
        {
            try
            {
                // Get current user ID (adjust based on your auth system)
                var userId = int.Parse(User.FindFirst("UsersId")?.Value);

                if (userId == 0)
                {
                    TempData["Error"] = "User not authenticated";
                    return RedirectToAction("Compose");
                }

                // Parse recipients
                var toRecipients = ParseRecipients(model.ToRecipients);
                var ccRecipients = ParseRecipients(model.CcRecipients);
                var bccRecipients = ParseRecipients(model.BccRecipients);

                // Validate
                if (!toRecipients.Any())
                {
                    TempData["Error"] = "Please add at least one recipient";
                    return RedirectToAction("Compose");
                }

                if (string.IsNullOrWhiteSpace(model.Subject))
                {
                    TempData["Error"] = "Subject is required";
                    return RedirectToAction("Compose");
                }

                // Get recipient user IDs from database
                var allEmails = toRecipients.Concat(ccRecipients).Concat(bccRecipients).Distinct().ToList();
                var recipients = await _context.Users
                    .Where(u => allEmails.Contains(u.Email))
                    .ToListAsync();

                if (recipients.Count != allEmails.Count)
                {
                    TempData["Error"] = "Some recipients were not found";
                    return RedirectToAction("Compose");
                }

                // Create Email Thread
                var thread = new EmailThread
                {
                    Subject = model.Subject,
                    CreatedBy = userId,
                    CreatedAt = DateTime.Now,
                    Creator = _context.Users.Find(userId)
                };
                _context.EmailThreads.Add(thread);
                await _context.SaveChangesAsync();

                // Create Email
                var email = new Email
                {
                    ThreadID = thread.Id,
                    SenderID = userId,
                    Body = model.Body,
                    SentAt = DateTime.Now,
                    IsDraft = false,
                    IsTrashed = false,
                    IsRead = true, // Sender has read it
                    IsStarred = false
                };
                _context.Emails.Add(email);
                await _context.SaveChangesAsync();

                // Add Recipients
                var emailRecipients = new List<EmailRecipient>();

                // TO recipients
                foreach (var toEmail in toRecipients)
                {
                    var user = recipients.First(r => r.Email == toEmail);
                    emailRecipients.Add(new EmailRecipient
                    {
                        EmailID = email.Id,
                        RecipientID = user.Id,
                        RecipientType = RecipientType.TO,
                        IsRead = false,
                        IsTrashed = false,
                        IsStarred = false
                    });
                }

                // CC recipients
                foreach (var ccEmail in ccRecipients)
                {
                    var user = recipients.First(r => r.Email == ccEmail);
                    emailRecipients.Add(new EmailRecipient
                    {
                        EmailID = email.Id,
                        RecipientID = user.Id,
                        RecipientType = RecipientType.CC,
                        IsRead = false,
                        IsTrashed = false,
                        IsStarred = false
                    });
                }

                // BCC recipients
                foreach (var bccEmail in bccRecipients)
                {
                    var user = recipients.First(r => r.Email == bccEmail);
                    emailRecipients.Add(new EmailRecipient
                    {
                        EmailID = email.Id,
                        RecipientID = user.Id,
                        RecipientType = RecipientType.BCC,
                        IsRead = false,
                        IsTrashed = false,
                        IsStarred = false
                    });
                }

                _context.EmailRecipients.AddRange(emailRecipients);
                await _context.SaveChangesAsync();

                // Handle Attachments
                if (files != null && files.Any())
                {
                    var attachments = await SaveAttachments(files, email.Id);
                    _context.EmailAttachments.AddRange(attachments);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Email sent successfully!";
                return RedirectToAction("Message");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error sending email: {ex.Message}";
                return RedirectToAction("Compose");
            }
        }

        public class ReplyEmailViewModel
        {
            public int Id { get; set; } // ThreadID
            public string ToRecipients { get; set; } = string.Empty;
            public string CcRecipients { get; set; } = string.Empty;
            public string BccRecipients { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
        }
        public class SendEmailViewModel
        {
            public string ToRecipients { get; set; } = string.Empty;
            public string CcRecipients { get; set; } = string.Empty;
            public string BccRecipients { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
        }
        [HttpPost]
        public async Task<IActionResult> SaveEditDraft(
    int id, // Email ID
    SendEmailViewModel model,
    List<IFormFile>? files)
        {
            var email = await _context.Emails
                .Include(e => e.Thread)
                .Include(e => e.Recipients)
                .Include(e => e.Attachments)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (email == null)
                return NotFound();

            // Update core email fields
            if (email.Thread != null)
                email.Thread.Subject = model.Subject;

            email.Body = model.Body;
            email.SentAt = DateTime.Now;

            // 🧹 Clear old recipients before updating
            email.Recipients.Clear();

            // ✅ Add new recipients
            var toRecipients = await ParseRecipients(model.ToRecipients, RecipientType.TO);
            var ccRecipients = await ParseRecipients(model.CcRecipients, RecipientType.CC);
            var bccRecipients = await ParseRecipients(model.BccRecipients, RecipientType.BCC);

            foreach (var r in toRecipients.Concat(ccRecipients).Concat(bccRecipients))
                email.Recipients.Add(r);

            // ✅ Handle file uploads
            if (files != null && files.Any())
            {
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "email-attachments");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    email.Attachments.Add(new EmailAttachment
                    {
                        FileName = fileName,
                        FilePath = "/uploads/email-attachments/" + fileName,
                        FileSize = file.Length
                    });
                }
            }

            _context.Update(email);
            await _context.SaveChangesAsync();

            return RedirectToAction("DraftEmail");
        }
        private async Task<List<EmailRecipient>> ParseRecipients(string? input, RecipientType type)
        {
            var recipients = new List<EmailRecipient>();

            if (!string.IsNullOrWhiteSpace(input))
            {
                var split = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var item in split)
                {
                    // Extract email from "Name <email@domain.com>"
                    var match = System.Text.RegularExpressions.Regex.Match(item, @"<(.+?)>");
                    var emailAddr = match.Success ? match.Groups[1].Value : item.Trim();

                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailAddr);
                    if (user != null)
                    {
                        recipients.Add(new EmailRecipient
                        {
                            RecipientID = user.Id,
                            RecipientType = type
                        });
                    }
                }
            }

            return recipients;
        }


        [HttpPost]
        public async Task<IActionResult> SaveDraft(SendEmailViewModel model, List<IFormFile> files)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

                // Create or update thread
                var thread = new EmailThread
                {
                    Subject = model.Subject ?? "No Subject",
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.Now
                };
                _context.EmailThreads.Add(thread);
                await _context.SaveChangesAsync();

                // Create draft email
                var email = new Email
                {
                    ThreadID = thread.Id,
                    SenderID = currentUserId,
                    Body = model.Body ?? string.Empty,
                    SentAt = DateTime.Now,
                    IsDraft = true,
                    IsTrashed = false,
                    IsRead = true,
                    IsStarred = false
                };
                _context.Emails.Add(email);
                await _context.SaveChangesAsync();

                // Save recipients if any
                var toRecipients = ParseRecipients(model.ToRecipients);
                var ccRecipients = ParseRecipients(model.CcRecipients);
                var bccRecipients = ParseRecipients(model.BccRecipients);

                if (toRecipients.Any() || ccRecipients.Any() || bccRecipients.Any())
                {
                    var allEmails = toRecipients.Concat(ccRecipients).Concat(bccRecipients).Distinct().ToList();
                    var recipients = await _context.Users
                        .Where(u => allEmails.Contains(u.Email))
                        .ToListAsync();

                    var emailRecipients = new List<EmailRecipient>();

                    foreach (var toEmail in toRecipients)
                    {
                        var user = recipients.FirstOrDefault(r => r.Email == toEmail);
                        if (user != null)
                        {
                            emailRecipients.Add(new EmailRecipient
                            {
                                EmailID = email.Id,
                                RecipientID = user.Id,
                                RecipientType = RecipientType.TO
                            });
                        }
                    }

                    foreach (var ccEmail in ccRecipients)
                    {
                        var user = recipients.FirstOrDefault(r => r.Email == ccEmail);
                        if (user != null)
                        {
                            emailRecipients.Add(new EmailRecipient
                            {
                                EmailID = email.Id,
                                RecipientID = user.Id,
                                RecipientType = RecipientType.CC
                            });
                        }
                    }

                    foreach (var bccEmail in bccRecipients)
                    {
                        var user = recipients.FirstOrDefault(r => r.Email == bccEmail);
                        if (user != null)
                        {
                            emailRecipients.Add(new EmailRecipient
                            {
                                EmailID = email.Id,
                                RecipientID = user.Id,
                                RecipientType = RecipientType.BCC
                            });
                        }
                    }

                    _context.EmailRecipients.AddRange(emailRecipients);
                    await _context.SaveChangesAsync();
                }

                // Handle Attachments
                if (files != null && files.Any())
                {
                    var attachments = await SaveAttachments(files, email.Id);
                    _context.EmailAttachments.AddRange(attachments);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Draft saved successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error saving draft: {ex.Message}";
                return RedirectToAction("Compose");
            }
        }
        [HttpPost]
        public async Task<JsonResult> TrashEmails([FromBody] List<int> recipientIds)
        {
            if (recipientIds == null || !recipientIds.Any())
                return Json(new { success = false, message = "No emails selected." });

            var currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

            try
            {
                var recipients = await _context.EmailRecipients
                    .Include(er => er.Email)
                        .ThenInclude(e => e.Thread)
                    .Where(er => recipientIds.Contains(er.Id))
                    .ToListAsync();

                foreach (var recipient in recipients)
                {
                    if (recipient.Email.Thread.CreatedBy == currentUserId)
                    {
                        recipient.Email.Thread.IsTrashed = true;
                    }
                    else
                    {
                        recipient.IsTrashed = true;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error." });
            }
        }
        [HttpGet]
        public async Task<JsonResult> SearchUsers(string query)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            var searchTerm = query.Trim().ToLower();

            var users = await _context.Users
                .Where(u => (u.Username.ToLower().Contains(searchTerm) ||
                            u.Email.ToLower().Contains(searchTerm)) && u.Email != userEmail)
                .Select(u => new
                {
                    value = u.Email,
                    fullName = u.Username,
                    email = u.Email
                })
                .Take(10)
                .ToListAsync();

            return Json(users);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(List<IFormFile> files)
        {
            try
            {
                var uploadedFiles = new List<object>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "email-attachments");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedFiles.Add(new
                        {
                            name = file.FileName,
                            size = file.Length,
                            path = $"/uploads/email-attachments/{uniqueFileName}"
                        });
                    }
                }

                return Json(new { success = true, files = uploadedFiles });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private List<string> ParseRecipients(string recipients)
        {
            if (string.IsNullOrWhiteSpace(recipients))
                return new List<string>();

            return recipients
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct()
                .ToList();
        }

        private async Task<List<EmailAttachment>> SaveAttachments(List<IFormFile> files, int emailId)
        {
            var attachments = new List<EmailAttachment>();
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "email-attachments");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    attachments.Add(new EmailAttachment
                    {
                        EmailID = emailId,
                        FileName = file.FileName,
                        FilePath = $"/uploads/email-attachments/{uniqueFileName}",
                        FileSize = file.Length
                    });
                }
            }

            return attachments;
        }
        public IActionResult JobApply(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Jobs
                        .Include(u => u.Users)
                            .ThenInclude(u => u.EmployerDetails)
                        .Include(u => u.JobBenefits)
                        .Include(u => u.JobRequirements)
                        .Include(u => u.JobMedia)
                        .Include(u => u.JobRole)
                        .Include(u => u.JobApplication.Where(a => a.AppliedBy == userId))
                        .Include(u => u.JobUploads.Where(a => a.isActive))
                        .Where(u => u.Id == Id && u.isArchived == null)
                        .FirstOrDefault();

            var userInfo = _context.JobApplication.Where(a => a.AppliedBy == userId).OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefault();

            var userProfile = _context.UserProfile.Where(a => a.UsersId == userId)
                        .FirstOrDefault();
            var viewModel = new JobApplyViewModel
            {
                Jobs = data,
                UserInfo = userInfo,
                UserProfile = userProfile
            };
            return View(viewModel);
        }

        public IActionResult SubmitReport(int? TrainingId, int? JobsId, string Reason, string Description)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var alreadyReported = _context.PostReport
                                    .Any(u => u.UsersId == userId &&
                                             ((TrainingId != null && u.TrainingId == TrainingId) ||
                                              (JobsId != null && u.JobsId == JobsId)));
            if(Reason != null && Description != null)
            {
                if (alreadyReported)
                {
                    TempData["Error"] = "Report already submitted";
                }
                else
                {
                    var report = new PostReport
                    {
                        UsersId = userId,
                        TrainingId = TrainingId,
                        JobsId = JobsId,
                        Reason = Reason,
                        Description = Description
                    };
                    _context.PostReport.Add(report);
                    _context.SaveChanges();
                    TempData["Success"] = "Report submitted successfully!";
                }
            }
            else
            {
                TempData["Error"] = "Fill up all the details!";
            }
            if(TrainingId != null)
            {
                return RedirectToAction("TrainingInfo", new { Id = TrainingId });
            }
            else
            {
                return RedirectToAction("JobInfo", new { Id = JobsId });
            }
               
        }
        public IActionResult TrainingApply(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var data = _context.Training
                        .Include(u => u.Users)
                            .ThenInclude(u => u.EmployerDetails)
                        .Include(u => u.Users)
                            .ThenInclude(u => u.UserProfile)
                        .Include(u => u.TrainingRequirements)
                        .Include(u => u.TrainingMedia)
                        .Include(u => u.TrainingBenefits)
                        .Include(u => u.TrainingBadge)
                        .Include(u => u.TrainingUploads.Where(u => u.isActive))
                        .Where(u => u.Id == Id && u.isArchived == null)
                        .FirstOrDefault();
            var userProfile = _context.UserProfile.Where(a => a.UsersId == userId)
                        .FirstOrDefault();

            var viewModel = new TrainingApplyViewModel
            {
                Training = data,
                UserProfile = userProfile
            };
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrainingApply(TrainingApplication model, int Id, IFormFile? ResumeFile, IFormFile? CoeFile, IFormFile? TorFile, IFormFile? SeamansBookFile, IFormFile? PassportIdFile, IFormFile? DiplomaFile, string? redirect)
        {
            ModelState.Keys
            .Where(k => k.EndsWith("TrainingUploads"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
            ModelState.Keys
            .Where(k => k.EndsWith("Upload"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
            ModelState.Remove("Jobs");
            ModelState.Remove("AppliedBy");
            ModelState.Remove("CV");
            ModelState.Remove("Training");

            model.TrainingId = Id;
            model.AppliedBy = int.Parse(User.FindFirst("UsersId")?.Value ?? throw new InvalidOperationException("User ID not found"));

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                TempData["error"] = "Please fill all the required fields!";
                return View(model);
            }

            // File upload logic (refactored for reuse)
            async Task<string> SaveFile(IFormFile? file)
            {
                if (file == null || file.Length == 0) return null;

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "requirements");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return "/uploads/requirements/" + uniqueFileName;
            }
            var trainingApplication = new TrainingApplication
            {
                TrainingId = model.TrainingId,
                AppliedBy = model.AppliedBy,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MobileNo = model.MobileNo,
                Age = model.Age,
                City = model.City,
                PaymentStatus = model.PaymentStatus
            };
            _context.TrainingApplication.Add(trainingApplication);
            await _context.SaveChangesAsync();

            foreach (var data in model.Uploads)
            {
                var uploads = new ApplicantTrainingUpload
                {
                    TrainingUploadsId = data.TrainingUploadsId,
                    UsersId = model.AppliedBy,
                    Upload = await SaveFile(data.File)
                };
                _context.ApplicantTrainingUpload.Add(uploads);
                await _context.SaveChangesAsync();
            }

            var trainingDesc = _context.Training
                .Include(t => t.Users)
                .ThenInclude(u => u.EmployerDetails)
                .FirstOrDefault(u => u.Id == model.TrainingId);

            var trainingPayment = new TrainingPayments
            {
                UsersId = model.AppliedBy,
                TrainingApplicationId = trainingApplication.Id,
                PaymentOption = trainingDesc.PaymentOption,
                Price = trainingDesc.Price,
                Paid = null,
                ModeOfPayment = trainingDesc.ModeOfPayment
            };

            _context.TrainingPayments.Add(trainingPayment);
            await _context.SaveChangesAsync();

            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var sendEmail = new EmailThread
            {
                Subject = "Thank you for applying!",
                CreatedBy = trainingDesc.UsersId,
                CreatedAt = DateTime.Now
            };
            _context.EmailThreads.Add(sendEmail);
            await _context.SaveChangesAsync();

            var email = new Email
            {
                Thread = sendEmail,
                SenderID = trainingDesc.UsersId,
                Body = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {
                            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            margin: 0;
                            padding: 0;
                            background-color: #f5f5f5;
                        }
                        .container {
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #ffffff;
                            border-radius: 8px;
                            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
                            overflow: hidden;
                        }
                        .header {
                            background: linear-gradient(135deg, #0066cc 0%, #0052a3 100%);
                            color: white;
                            padding: 40px 20px;
                            text-align: center;
                        }
                        .header h1 {
                            margin: 0;
                            font-size: 28px;
                            font-weight: 600;
                        }
                        .content {
                            padding: 40px;
                        }
                        .greeting {
                            font-size: 16px;
                            margin-bottom: 20px;
                            color: #333;
                        }
                        .highlight-box {
                            background-color: #e8f4f8;
                            border-left: 4px solid #0066cc;
                            padding: 20px;
                            margin: 25px 0;
                            border-radius: 4px;
                        }
                        .highlight-box p {
                            margin: 8px 0;
                            font-size: 15px;
                        }
                        .highlight-label {
                            font-weight: 600;
                            color: #0066cc;
                            font-size: 12px;
                            text-transform: uppercase;
                            letter-spacing: 0.5px;
                        }
                        .highlight-value {
                            font-size: 18px;
                            font-weight: 600;
                            color: #333;
                            margin-top: 5px;
                        }
                        .body-text {
                            font-size: 15px;
                            line-height: 1.7;
                            color: #555;
                            margin-bottom: 20px;
                        }
                        .footer {
                            background-color: #f9f9f9;
                            padding: 20px;
                            border-top: 1px solid #e0e0e0;
                            text-align: center;
                            font-size: 12px;
                            color: #888;
                        }
                        .divider {
                            height: 1px;
                            background-color: #e0e0e0;
                            margin: 30px 0;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>✓ Application Received</h1>
                        </div>
                        <div class='content'>
                            <p class='greeting'>Dear " + model.FirstName + @" " + model.LastName + @",</p>
                            <p class='body-text'>Thank you for applying to the <strong>" + trainingDesc.Name + @"</strong> training program at <strong>" + trainingDesc.Users.EmployerDetails.BusinessName + @"</strong>. We've received your application and appreciate your interest.</p>
                            
                            <div class='highlight-box'>
                                <p class='highlight-label'>What's Next?</p>
                                <p class='body-text' style='margin: 10px 0; font-size: 14px;'>Our team is currently reviewing applications. If your qualifications match what we're looking for, we'll reach out to schedule an interview.</p>
                            </div>
                            
                            <p class='body-text'>In the meantime, feel free to explore more about " + trainingDesc.Users.EmployerDetails.BusinessName + @" and learn about our training programs and values.</p>
                            
                            <div class='divider'></div>
                            <p class='body-text' style='font-size: 13px; color: #888;'>Best regards,<br><strong>The " + trainingDesc.Users.EmployerDetails.BusinessName + @" Team</strong></p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message from " + trainingDesc.Users.EmployerDetails.BusinessName + @". Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>
        ",
                SentAt = DateTime.Now,
                IsDraft = false,
                IsTrashed = false,
                IsRead = false,
                IsStarred = false
            };
            _context.Emails.Add(email);
            await _context.SaveChangesAsync();

            var emailRecipient = new EmailRecipient
            {
                EmailID = email.Id,
                RecipientID = userId,
                RecipientType = RecipientType.TO,
                IsRead = false,
                IsTrashed = false,
                IsStarred = false
            };
            _context.EmailRecipients.Add(emailRecipient);
            await _context.SaveChangesAsync();

            if (redirect == null)
            {
                TempData["success"] = "Application submitted successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("TrainingPayment", new { Id = trainingPayment.Id });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JobApply(JobApplication model, int Id, IFormFile? ResumeFile, IFormFile? CoeFile, IFormFile? TorFile, IFormFile? SeamansBookFile, IFormFile? PassportIdFile, IFormFile? DiplomaFile)
        {
            // Remove unrelated properties from ModelState

            ModelState.Keys
            .Where(k => k.EndsWith("JobUploads"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
            ModelState.Keys
            .Where(k => k.EndsWith("Upload"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
            ModelState.Remove("Jobs");
            ModelState.Remove("AppliedBy");
            ModelState.Remove("CV");
            ModelState.Remove("Experience");

            model.JobsId = Id;
            model.AppliedBy = int.Parse(User.FindFirst("UsersId")?.Value);

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                TempData["error"] = "Please fill all the required fields!";
                return View(Id);
            }

            // File upload logic (refactored for reuse)
            async Task<string> SaveFile(IFormFile? file)
            {
                if (file == null || file.Length == 0) return null;

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "requirements");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return "/uploads/requirements/" + uniqueFileName;
            }

            var jobApplication = new JobApplication
            {
                JobsId = model.JobsId,
                AppliedBy = model.AppliedBy,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MobileNo = model.MobileNo,
                Age = model.Age,
                City = model.City,
                ExpectedSalary = model.ExpectedSalary,
                StartDate = model.StartDate,
                Experience = model.Experience ?? "",
                ApplicationLetter = model.ApplicationLetter
            };

            _context.JobApplication.Add(jobApplication);
            await _context.SaveChangesAsync();


            foreach (var data in model.Uploads)
            {
                var uploads = new ApplicantJobUpload
                {
                    JobUploadsId = data.JobUploadsId,
                    UsersId = model.AppliedBy,
                    Upload = await SaveFile(data.File)
                };
                _context.ApplicantJobUpload.Add(uploads);
                await _context.SaveChangesAsync();
            }
            var job = await _context.Jobs
           .Include(j => j.Users)
            .ThenInclude(j => j.EmployerDetails)
           .FirstOrDefaultAsync(j => j.Id == Id);

            // Get the applicant user details
            var applicant = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.AppliedBy);

            // Get the "Applied" email template
            var emailTemplate = await _context.EmailTemplate
                .FirstOrDefaultAsync(et => et.TypeOfEmail == "Applied");

            if (emailTemplate != null && applicant != null)
            {
                // Replace template variables
                string subject = emailTemplate.Subject
                    .Replace("{{ApplicantName}}", model.FirstName + " " + model.LastName)
                    .Replace("{{JobTitle}}", job.JobTitle)
                    .Replace("{{CompanyName}}", job.Users.EmployerDetails.BusinessName);

                string body = emailTemplate.Body
                    .Replace("{{ApplicantName}}", model.FirstName + " " + model.LastName)
                    .Replace("{{JobTitle}}", job.JobTitle)
                    .Replace("{{CompanyName}}", job.Users.EmployerDetails.BusinessName);
                var userId = int.Parse(User.FindFirst("UsersId")?.Value);
                var sendEmail = new EmailThread
                {
                    Subject = subject,
                    CreatedBy = job.UsersId,
                    CreatedAt = DateTime.Now
                };
                _context.EmailThreads.Add(sendEmail);
                await _context.SaveChangesAsync();
                var email = new Email
                {
                    Thread = sendEmail,
                    SenderID = job.UsersId,
                    Body = body,
                    SentAt = DateTime.Now,
                    IsDraft = false,
                    IsTrashed = false,
                    IsRead = false,
                    IsStarred = false
                };
                _context.Emails.Add(email);
                await _context.SaveChangesAsync();
                var emailRecipient = new EmailRecipient
                {
                    EmailID = email.Id,
                    RecipientID = userId,
                    RecipientType = RecipientType.TO,
                    IsRead = false,
                    IsTrashed = false,
                    IsStarred = false
                };
                _context.EmailRecipients.Add(emailRecipient);
                await _context.SaveChangesAsync();
            }
            TempData["success"] = "Application submitted successfully!";
            return RedirectToAction("Index");
        }
        public IActionResult Applications()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var applications = _context.JobApplication
                .Where(a => a.AppliedBy == userId)
                .Include(a => a.Jobs)
                    .ThenInclude(a => a.Users)
                .ToList();

            return View(applications);
        }
        public IActionResult TrainingApplicationDetails(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Training
                       .Include(u => u.Users)
                           .ThenInclude(u => u.EmployerDetails)
                       .Include(u => u.TrainingBenefits)
                       .Include(u => u.TrainingApplication.Where(a => a.AppliedBy == userId))
                            .ThenInclude(u => u.TrainingCertificate)
                       .Include(u => u.TrainingRequirements)
                       .Include(u => u.TrainingMedia)
                       .Include(u => u.TrainingUploads.Where(a => a.isActive))
                           .ThenInclude(u => u.ApplicantTrainingUpload.Where(u => u.UsersId == userId))
                       .Where(u => u.Id == Id && u.isArchived == null)
                       .FirstOrDefault();

            if (data == null)
            {
                return NotFound(); // or redirect to an error page
            }

            return View(data);
        }
        public IActionResult ApplicationDetails(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Jobs
                       .Include(u => u.Users)
                           .ThenInclude(u => u.EmployerDetails)
                       .Include(u => u.JobBenefits)
                       .Include(u => u.JobApplication.Where(a => a.AppliedBy == userId))
                       .Include(u => u.JobRequirements)
                       .Include(u => u.JobRole)
                       .Include(u => u.JobMedia)
                       .Include(u => u.JobUploads.Where(a => a.isActive))
                           .ThenInclude(u => u.ApplicantJobUpload.Where(u => u.UsersId == userId))
                       .Where(u => u.Id == Id && u.isArchived == null)
                       .FirstOrDefault();

            if (data == null)
            {
                return NotFound(); // or redirect to an error page
            }

            return View(data);
        }
        public IActionResult ChangeProfileDetails(string Firstname, string Lastname, string Nationality, string Birthday, string MobileNumber)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var user = _context.UserProfile
                        .FirstOrDefault(u => u.UsersId == userId);
            DateOnly parsedBirthday = DateOnly.Parse(Birthday);
            if (user == null)
            {
                var users = new UserProfile
                {
                    FirstName = Firstname,
                    LastName = Lastname,
                    City = Nationality,
                    Birthdate = parsedBirthday,
                    MobileNo = MobileNumber,
                    UsersId = userId
                };


                _context.UserProfile.Add(users);
                _context.SaveChanges();
            }
            else
            {
                user.FirstName = Firstname;
                user.LastName = Lastname;
                user.City = Nationality;
                user.Birthdate = parsedBirthday;
                user.MobileNo = MobileNumber;

                _context.UserProfile.Update(user);
                _context.SaveChanges();

            }

                return Json(new { response = true });
        }

        public IActionResult ChangePassword(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return Json(new { response = false, message = "User not found." });

            var passwordHasher = new PasswordHasher<Users>();
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, CurrentPassword);

            if (result != PasswordVerificationResult.Success)
                return Json(new { response = false, message = "Current password is incorrect." });

            if (NewPassword != ConfirmNewPassword)
                return Json(new { response = false, message = "Passwords do not match." });

            user.Password = passwordHasher.HashPassword(user, NewPassword);
            _context.Users.Update(user);
            _context.SaveChanges();

            return Json(new { response = true });
        }
        public IActionResult AccountSettings()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var userProfile = _context.Users
                .Include(u => u.TrainingApplication.Where(u => u.AppliedBy == userId))
                .Include(u => u.JobApplication.Where(u => u.AppliedBy == userId))
                .Include(u => u.EmployeeDetails)
                .Include(u => u.UserProfile)
                .Include(u => u.UserBadge)
                .Where(u => u.Id == userId).FirstOrDefault();

            var interviewCount = _context.JobApplication
                .Count(u => u.AppliedBy == userId && (u.Status == "For Interview" || u.Status == "Interview"));

            var totalCount = _context.JobApplication
                .Count(u => u.AppliedBy == userId);

            decimal percent = totalCount == 0
                ? 0
                : (decimal)interviewCount / totalCount * 100;
            var today = DateTime.Today;

            // New job applications today
            var newJobsToday = _context.JobApplication
                .Count(j => j.AppliedBy == userId && j.CreatedAt.Date == today);

            // New training applications today
            var newTrainingsToday = _context.TrainingApplication
                .Count(t => t.AppliedBy == userId && t.CreatedAt.Date == today);

            // New interviews today
            var interviewsToday = _context.JobApplication
                .Count(j => j.AppliedBy == userId
                         && (j.Status == "For Interview" || j.Status == "Interview")
                         && j.CreatedAt.Date == today);

            // Flags
            ViewBag.HasNewJobs = newJobsToday > 0;
            ViewBag.HasNewTrainings = newTrainingsToday > 0;
            ViewBag.HasNewInterviews = interviewsToday > 0;
            ViewBag.InterviewPercentage = percent.ToString("N0");

            return View(userProfile);
        }

    }
}
