using burbodek.Data;
using burbodek.Migrations;
using burbodek.Models;
using burbodek.Models.ViewModels;
using burbodek.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        ApplicationDbContext _context;
        private readonly EmailServices _email;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, EmailServices email, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _email = email;
            _env = env;
        }
        public IActionResult Index()
        {
            var today = DateTime.Today;
            var weekAgo = today.AddDays(-6);

            // Daily revenue — sum Plans.Price per day
            var dailyRevenue = _context.Subscription
                .Include(s => s.Plans)
                .Where(s => s.CreatedAt.Date >= weekAgo)
                .AsEnumerable() // switch to client evaluation for nullable double->decimal
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new DailyRevenueItem
                {
                    Date = g.Key,
                    Total = g.Sum(s => (decimal)(s.Plans?.Price ?? 0))
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Fill in missing days with 0
            for (int i = 0; i <= 6; i++)
            {
                var day = weekAgo.AddDays(i);
                if (!dailyRevenue.Any(d => d.Date == day))
                    dailyRevenue.Add(new DailyRevenueItem { Date = day, Total = 0 });
            }
            dailyRevenue = dailyRevenue.OrderBy(d => d.Date).ToList();

            var vm = new AdminDashboardViewModel
            {
                // Revenue
                TodayRevenue = _context.Subscription
                    .Include(s => s.Plans)
                    .Where(s => s.CreatedAt.Date == today)
                    .AsEnumerable()
                    .Sum(s => (decimal)(s.Plans?.Price ?? 0)),

                WeeklyRevenue = _context.Subscription
                    .Include(s => s.Plans)
                    .Where(s => s.CreatedAt.Date >= weekAgo)
                    .AsEnumerable()
                    .Sum(s => (decimal)(s.Plans?.Price ?? 0)),

                // Subscriptions
                ActiveSubscriptions = _context.Subscription
                    .Count(s => s.Status == "Current" && s.Expiration >= DateTime.Now),

                // New Users
                NewUsersThisWeek = _context.Users
                    .Count(u => u.DateCreated >= weekAgo),

                NewUsers = _context.Users
                    .Include(u => u.UserProfile)
                    .Where(u => u.DateCreated >= weekAgo)
                    .OrderByDescending(u => u.DateCreated)
                    .ToList(),

                // Latest Subscriptions
                LatestSubscriptions = _context.Subscription
                    .Include(s => s.Users)
                        .ThenInclude(u => u.UserProfile)
                    .Include(s => s.Plans)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(10)
                    .ToList(),

                // Daily Revenue
                DailyRevenue = dailyRevenue,

                // Campaigns
                TotalCampaigns = _context.Campaign.Count(),

                ActiveCampaigns = _context.Campaign
                    .Count(c => c.IsActive && c.isPaid == true),

                CampaignRevenue = _context.Campaign
                    .Where(c => c.isPaid == true)
                    .Sum(c => c.Payment ?? 0),

                LatestCampaigns = _context.Campaign
                    .Include(c => c.CreatedByUser)
                        .ThenInclude(u => u.UserProfile)
                    .Include(c => c.SelectedJob)
                    .Include(c => c.SelectedTraining)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList()
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Terms()
        {
            var terms =_context.Terms.Where(t => t.Id == 1).FirstOrDefault();
            return View(terms);
        }
        [HttpPost]
        public IActionResult Terms(string Description)
        {
            var data = _context.Terms.Where(t => t.Id == 1).FirstOrDefault();
            if (Description != null)
            {
                if(data != null)
                {
                    data.Description = Description;
                    _context.SaveChanges();
                    TempData["Success"] = "Terms added successfully!";
                    return View();
                }
                else
                {
                    var terms = new Terms
                    {
                        Description = Description
                    };
                    _context.Terms.Add(terms);
                    _context.SaveChanges();
                    TempData["Success"] = "Terms added successfully!";
                    return View();
                }
            }
            return View();
        }
        public IActionResult Users()
        {
            var data = _context.Users.Include(u => u.UserProfile).Where(u => u.Role == "Client").ToList();
            return View(data);
        }
        public IActionResult Faq()
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
        public IActionResult Subscription()
        {
            var plan = _context.Plans.ToList();
            return View(plan);
        }
        [HttpPost]
        public IActionResult UpdateSubscription(int Id, string PlanName, string PlanDetails, double Price, int Discount)
        {
            try
            {
                var plan = _context.Plans.FirstOrDefault(p => p.Id == Id);

                if (plan == null)
                {
                    return Json(new { success = false, message = "Plan not found" });
                }

                plan.PlanName = PlanName;
                plan.PlanDetails = PlanDetails;
                plan.Price = Price;
                plan.Discount = Discount;

                _context.SaveChanges();

                return Json(new { success = true, message = "Plan updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult UpdateFaq(int Id, string Title, string Description)
        {
            try
            {
                var faq = _context.Faq.FirstOrDefault(f => f.Id == Id);
                if (faq == null)
                    return Json(new { success = false, message = "FAQ not found" });

                faq.Title = Title;
                faq.Description = Description;
                _context.SaveChanges();

                return Json(new { success = true, message = "FAQ updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteFaq(int Id)
        {
            try
            {
                var faq = _context.Faq.FirstOrDefault(f => f.Id == Id);
                if (faq == null)
                    return Json(new { success = false, message = "FAQ not found" });

                _context.Faq.Remove(faq);
                _context.SaveChanges();

                return Json(new { success = true, message = "FAQ deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult SubmitFaq(string Title, string Description)
        {
            if(Title != null && Description != null)
            {
                var faq = new Faq
                {
                    Title = Title,
                    Description = Description,
                    isActive = true
                };
                _context.Faq.Add(faq);
                _context.SaveChanges();
                return Json(new { success = true, message = "Added successfully!" });
            }
            return Json(new { success = false, message = "Faq adding failed!" });

        }
        public IActionResult SubmitFaqDescription(string Description)
        {
            if (Description != null)
            {
                var faq = new FaqTitle
                {
                    Description = Description
                };
                _context.FaqTitle.Add(faq);
                _context.SaveChanges();
                return Json(new { success = true, message = "Added successfully!" });
            }
            return Json(new { success = false, message = "Faq adding failed!" });

        }
        public IActionResult Application()
        {
            var application = _context.Users
                .Include(e => e.EmployerDetails)
                .Include(e => e.Subscription.Where(u => u.Status == "Current"))
                .ThenInclude(u => u.Plans)
                .Where(e => e.Role == "Employer")
                .ToList();

            return View(application);
        }

        public IActionResult ApplicationDetails(int id)
        {
            var employer = _context.Users
                .Include(e => e.EmployerDetails)
                .Include(e => e.Subscription)
                .Include(e => e.Files)
                .FirstOrDefault(e => e.Id == id);

            employer.Files = _context.Files
                .Where(f => f.UsersId == employer.Id)
                .ToList();
            employer.Subscription = _context.Subscription
                .Where(s => s.UsersId == employer.Id)
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
        public IActionResult Appeal()
        {
            var data = _context.PostReport
                    .Include(u => u.Users)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.Users)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobBenefits)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobRequirements)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobMedia)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobRequiredBadge)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobRole)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.Users)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingRequirements)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingBadge)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingBenefits)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingMedia)
                    .Where(u => (u.isDeleted == true && u.isRetained != true) && (u.JobsId != null || u.TrainingId != null) && (u.Jobs.isFinal == null && u.Training.isFinal == null)).OrderByDescending(u => u.DateReported).Distinct().ToList();
            return View(data);
        }
        [HttpPost]
        public async Task<IActionResult> RemoveFinal(int id)
        {
            try
            {
                var report = await _context.PostReport
                    .Include(r => r.Jobs)
                        .ThenInclude(j => j.JobMedia)
                    .Include(r => r.Training)
                        .ThenInclude(t => t.TrainingMedia)
                    .FirstOrDefaultAsync(r => r.Id == id);
                var isJobs = await _context.PostReport.Where(r => r.JobsId == report.JobsId && r.JobsId != null).ToListAsync();
                var isTraining = await _context.PostReport.Where(r => r.TrainingId == report.TrainingId && r.TrainingId != null).ToListAsync();
                if (report == null)
                    return Json(new { success = false, message = "Report not found." });
                if (isJobs.Count > 0)
                {
                    foreach (var job in isJobs)
                    {
                        var jobUpdated = _context.Jobs.Where(u => u.Id == report.JobsId).FirstOrDefault();
                        jobUpdated.isFinal = true;
                    }
                }
                if (isTraining.Count > 0)
                {
                    foreach (var train in isTraining)
                    {
                        var trainUpdated = _context.Training.Where(u => u.Id == report.TrainingId).FirstOrDefault();
                        trainUpdated.isFinal = true;
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            try
            {
                var report = await _context.PostReport
                    .Include(r => r.Jobs)
                        .ThenInclude(j => j.JobMedia)
                    .Include(r => r.Training)
                        .ThenInclude(t => t.TrainingMedia)
                    .FirstOrDefaultAsync(r => r.Id == id);
                var isJobs = await _context.PostReport.Where(r => r.JobsId == report.JobsId && r.JobsId != null).ToListAsync();
                var isTraining = await _context.PostReport.Where(r => r.TrainingId == report.TrainingId && r.TrainingId != null).ToListAsync();
                if (report == null)
                    return Json(new { success = false, message = "Report not found." });
                if (isJobs.Count > 0)
                {
                    foreach (var job in isJobs)
                    {
                        var jobUpdated = _context.Jobs.Where(u => u.Id == report.JobsId).FirstOrDefault();
                        job.isDeleted = true;
                        jobUpdated.isDeleted = true;
                    }
                }
                if (isTraining.Count > 0)
                {
                    foreach (var train in isTraining)
                    {
                        var trainUpdated = _context.Training.Where(u => u.Id == report.TrainingId).FirstOrDefault();
                        train.isDeleted = true;
                        trainUpdated.isDeleted = true;
                    }
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Retain(int id)
        {
            try
            {
                var report = await _context.PostReport.FirstOrDefaultAsync(r => r.Id == id);
                var isJobs = await _context.PostReport.Where(r => r.JobsId == report.JobsId && r.JobsId != null).ToListAsync();
                var isTraining = await _context.PostReport.Where(r => r.TrainingId == report.TrainingId && r.TrainingId != null).ToListAsync();

                if (report == null)
                    return Json(new { success = false, message = "Report not found." });
                if (isJobs.Count > 0)
                {
                    foreach (var job in isJobs)
                    {
                        var jobUpdated = _context.Jobs.Where(u => u.Id == report.JobsId).FirstOrDefault();
                        job.isRetained = true;
                        jobUpdated.isDeleted = null;
                    }
                }
                if (isTraining.Count > 0)
                {
                    foreach (var train in isTraining)
                    {
                        var trainUpdated = _context.Training.Where(u => u.Id == report.TrainingId).FirstOrDefault();
                        train.isRetained = true;
                        trainUpdated.isDeleted = null;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public IActionResult Reports()
        {
            var data = _context.PostReport
                    .Include(u => u.Users)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.Users)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobBenefits)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobRequirements)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobMedia)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobRequiredBadge)
                    .Include(u => u.Jobs)
                        .ThenInclude(u => u.JobRole)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.Users)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingRequirements)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingBadge)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingBenefits)
                    .Include(u => u.Training)
                        .ThenInclude(u => u.TrainingMedia)
                    .Where(u => u.isDeleted == null && u.isRetained == null).OrderByDescending(u => u.DateReported).ToList();
            return View(data);
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private string LoadTemplate(string fileName)
        {
            var path = Path.Combine(_env.ContentRootPath, "EmailTemplates", fileName);
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException("Email template not found", path);
            }
            return System.IO.File.ReadAllText(path);
        }
        [HttpPost]
        public async Task<IActionResult> ApplicationApproval(
            int SubscriptionId,
            int Id,
            string ApprovalDetails,
            string? DeclineReason,
            bool? isAllowed,
            bool IsBusinessName,
            bool IsBusinessDescription,
            bool IsSecDti,
            bool IsBirCertificate,
            bool IsBusinessPermit,
            bool IsPoeaLicense,
            bool IsProofPartnerShip)
        {
            try
            {
                var employer = _context.EmployerDetails
                    .Include(u => u.Users)
                    .Where(u => u.UsersId == Id)
                    .FirstOrDefault();

                if (employer == null)
                {
                    TempData["error"] = "Employer not found.";
                    return RedirectToAction("Application");
                }

                if (ApprovalDetails == "decline" && DeclineReason != null)
                {
                    var data = _context.Subscription.Where(e => e.Id == SubscriptionId).FirstOrDefault();

                    string emailBody = LoadTemplate("Declined.html");

                    string resubmitMessage = isAllowed == true
                        ? "You may submit a new employer application after correcting the issues mentioned above."
                        : "At this time, resubmission of your employer application is not allowed.";

                    // Build document checklist section
                    string documentSection = "";
                    if (isAllowed == true)
                    {
                        var flaggedDocs = new List<string>();

                        if (IsBusinessName) flaggedDocs.Add("Business Name");
                        if (IsBusinessDescription) flaggedDocs.Add("Business Description");
                        if (IsSecDti) flaggedDocs.Add("SEC / DTI Certificate");
                        if (IsBirCertificate) flaggedDocs.Add("BIR Certificate");
                        if (IsBusinessPermit) flaggedDocs.Add("Business Permit");
                        if (IsPoeaLicense) flaggedDocs.Add("POEA License");
                        if (IsProofPartnerShip) flaggedDocs.Add("Proof of Partnership");

                        if (flaggedDocs.Any())
                        {
                            var docRows = string.Join("", flaggedDocs.Select(doc => $@"
                <tr>
                    <td style='padding:10px 12px;border-bottom:1px solid #fed7d7;'>
                        <span style='display:inline-block;width:10px;height:10px;background:#e53e3e;border-radius:50%;margin-right:10px;'></span>
                        <span style='color:#2d3748;font-size:14px;'>{doc}</span>
                    </td>
                </tr>
            "));

                            documentSection = $@"
                <div style='margin:30px 0;'>
                    <div style='background:#fff5f5;border:1px solid #fed7d7;border-radius:8px;overflow:hidden;'>
                        <div style='background:#e53e3e;padding:14px 20px;'>
                            <p style='margin:0;color:#ffffff;font-size:15px;font-weight:bold;'>
                                📋 Documents Requiring Resubmission
                            </p>
                        </div>
                        <div style='padding:16px 20px 8px;'>
                            <p style='margin:0 0 12px;color:#742a2a;font-size:13px;line-height:1.6;'>
                                Please prepare and resubmit the following documents when reapplying:
                            </p>
                        </div>
                        <table role='presentation' style='width:100%;border-collapse:collapse;'>
                            {docRows}
                        </table>
                        <div style='padding:16px 20px;'>
                            <p style='margin:0;color:#742a2a;font-size:13px;line-height:1.6;'>
                                ⚠️ Please ensure all documents are <strong>clear, valid, and up to date</strong> before resubmitting your application.
                            </p>
                        </div>
                    </div>
                </div>
            ";
                        }

                        emailBody = emailBody.Replace("{{RejectionReason}}", DeclineReason);
                        emailBody = emailBody.Replace("{{ResubmitMessage}}", resubmitMessage);
                        emailBody = emailBody.Replace("{{DocumentSection}}", documentSection);

                        await _email.SendEmailAsync(employer.Users.Email, "Application Declined", emailBody);


                        // Update subscription
                        data.Expiration = DateTime.Now;
                        data.Status = "Expired";
                        _context.Subscription.Update(data);

                        // Update employer status
                        employer.RejectionReason = DeclineReason;
                        employer.Status = "Decline";
                        employer.isAllowedForResubmission = isAllowed;
                            }
                    if (isAllowed == true)
                    {
                        employer.RegistrationCount += 1;

                        // Only save document flags when resubmission is allowed
                        employer.IsBusinessName = IsBusinessName;
                        employer.IsBusinessDescription = IsBusinessDescription;
                        employer.IsSecDti = IsSecDti;
                        employer.IsBirCertificate = IsBirCertificate;
                        employer.IsBusinessPermit = IsBusinessPermit;
                        employer.IsPoeaLicense = IsPoeaLicense;
                        employer.IsProofPartnerShip = IsProofPartnerShip;
                    }
                    else
                    {
                        // Clear all flags if resubmission is not allowed
                        employer.IsBusinessName = null;
                        employer.IsBusinessDescription = null;
                        employer.IsSecDti = null;
                        employer.IsBirCertificate = null;
                        employer.IsBusinessPermit = null;
                        employer.IsPoeaLicense = null;
                        employer.IsProofPartnerShip = null;
                    }

                    _context.EmployerDetails.Update(employer);
                    await _context.SaveChangesAsync();

                    TempData["success"] = "Employer declined.";
                    return RedirectToAction("Index");
                }
                else if (ApprovalDetails == "approve")
                {
                    var data = _context.Subscription.Where(e => e.Id == SubscriptionId).FirstOrDefault();

                    // Load approval email template
                    string emailBody = LoadTemplate("Approved.html");
                    await _email.SendEmailAsync(employer.Users.Email, "Employer Approved!", emailBody);

                    // Update subscription
                    data.Expiration = null;
                    data.Status = "Current";
                    _context.Subscription.Update(data);

                    // Update employer status and clear all flags on approval
                    employer.Status = "Approved";
                    employer.RejectionReason = null;
                    employer.isAllowedForResubmission = null;
                    employer.IsBusinessName = null;
                    employer.IsBusinessDescription = null;
                    employer.IsSecDti = null;
                    employer.IsBirCertificate = null;
                    employer.IsBusinessPermit = null;
                    employer.IsPoeaLicense = null;
                    employer.IsProofPartnerShip = null;

                    _context.EmployerDetails.Update(employer);
                    await _context.SaveChangesAsync();

                    TempData["success"] = "Employer successfully approved!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["error"] = "Unexpected error occurred.";
                    return RedirectToAction("Application");
                }
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}<br><br>Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
