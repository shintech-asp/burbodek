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
        private void AddDefaultEmailTemplates(int employerId)
        {
            var existingTemplates = _context.EmailTemplate
                .Where(t => t.UsersId == employerId)
                .Select(t => t.TypeOfEmail)
                .ToList();
            var templates = new List<EmailTemplate>();

            if (!existingTemplates.Contains("Applied"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "Applied",
                    Subject = "Your job application has been received!",
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
                                .cta-button {
                                    display: inline-block;
                                    background-color: #0066cc;
                                    color: white;
                                    padding: 12px 30px;
                                    text-decoration: none;
                                    border-radius: 4px;
                                    font-weight: 600;
                                    margin: 20px 0;
                                    font-size: 14px;
                                }
                                .cta-button:hover {
                                    background-color: #0052a3;
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
                                    <h1>Application Received</h1>
                                </div>
                                <div class='content'>
                                    <p class='greeting'>Dear {{ApplicantName}},</p>
                                    <p class='body-text'>Thank you for applying to the <strong>{{JobTitle}}</strong> position at <strong>{{CompanyName}}</strong>. We've received your application and appreciate your interest in joining our team.</p>
            
                                    <div class='highlight-box'>
                                        <p class='highlight-label'>What's Next?</p>
                                        <p class='body-text' style='margin: 10px 0; font-size: 14px;'>Our hiring team is currently reviewing applications. If your qualifications match what we're looking for, we'll reach out to schedule an interview.</p>
                                    </div>
            
                                    <p class='body-text'>In the meantime, feel free to explore more about {{CompanyName}} and learn about our culture and values.</p>
            
                                    <div class='divider'></div>
                                    <p class='body-text' style='font-size: 13px; color: #888;'>Best regards,<br><strong>The {{CompanyName}} Team</strong></p>
                                </div>
                                <div class='footer'>
                                    <p>This is an automated message from {{CompanyName}}. Please do not reply to this email.</p>
                                </div>
                            </div>
                        </body>
                        </html>
                                ",
                    UsersId = employerId,
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true
                });
            }

            if (!existingTemplates.Contains("For Interview"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "For Interview",
                    Subject = "Interview Invitation for {{JobTitle}}",
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
                                    background: linear-gradient(135deg, #28a745 0%, #1e7e34 100%);
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
                                    background-color: #e8f5e9;
                                    border-left: 4px solid #28a745;
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
                                    color: #28a745;
                                    font-size: 12px;
                                    text-transform: uppercase;
                                    letter-spacing: 0.5px;
                                }
                                .body-text {
                                    font-size: 15px;
                                    line-height: 1.7;
                                    color: #555;
                                    margin-bottom: 20px;
                                }
                                .cta-button {
                                    display: inline-block;
                                    background-color: #28a745;
                                    color: white;
                                    padding: 12px 30px;
                                    text-decoration: none;
                                    border-radius: 4px;
                                    font-weight: 600;
                                    margin: 20px 0;
                                    font-size: 14px;
                                }
                                .cta-button:hover {
                                    background-color: #1e7e34;
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
                                    <h1>🎤 You're Invited to Interview!</h1>
                                </div>
                                <div class='content'>
                                    <p class='greeting'>Dear {{ApplicantName}},</p>
                                    <p class='body-text'>Congratulations! We were impressed with your application and would like to move forward to the next stage. We're pleased to invite you to interview for the <strong>{{JobTitle}}</strong> position at <strong>{{CompanyName}}</strong>.</p>
            
                                    <div class='highlight-box'>
                                        <p class='highlight-label'>Interview Details</p>
                                        <p style='margin: 10px 0; font-size: 14px; color: #333;'><strong>Position:</strong> {{JobTitle}}</p>
                                        <p style='margin: 10px 0; font-size: 14px; color: #555;'>Our hiring team will contact you shortly with specific details about the interview format, date, and time. Please ensure your contact information is up to date.</p>
                                    </div>
            
                                    <p class='body-text'>If you have any questions before the interview, please don't hesitate to reach out. We look forward to learning more about you!</p>
            
                                    <div style='text-align: center;'>
                                        <a href='#' class='cta-button'>Confirm Interview</a>
                                    </div>
            
                                    <div class='divider'></div>
                                    <p class='body-text' style='font-size: 13px; color: #888;'>Best regards,<br><strong>The {{CompanyName}} Team</strong></p>
                                </div>
                                <div class='footer'>
                                    <p>This is an automated message from {{CompanyName}}. Please do not reply to this email.</p>
                                </div>
                            </div>
                        </body>
                        </html>
                                ",
                    UsersId = employerId,
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true
                });
            }

            if (!existingTemplates.Contains("Rejected"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "Rejected",
                    Subject = "Update on your {{JobTitle}} application",
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
                                        background: linear-gradient(135deg, #6c757d 0%, #5a6268 100%);
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
                                        background-color: #f8f9fa;
                                        border-left: 4px solid #6c757d;
                                        padding: 20px;
                                        margin: 25px 0;
                                        border-radius: 4px;
                                    }
                                    .highlight-box p {
                                        margin: 8px 0;
                                        font-size: 15px;
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
                                        <h1>Application Update</h1>
                                    </div>
                                    <div class='content'>
                                        <p class='greeting'>Dear {{ApplicantName}},</p>
                                        <p class='body-text'>Thank you for your interest in the <strong>{{JobTitle}}</strong> position at <strong>{{CompanyName}}</strong> and for taking the time to apply. We appreciate the effort you put into your application.</p>
            
                                        <div class='highlight-box'>
                                            <p style='margin: 0; font-size: 15px; color: #333;'>After careful consideration, we have decided not to move forward with your application at this time. This decision does not reflect your qualifications, but rather the specific needs of this particular role.</p>
                                        </div>
            
                                        <p class='body-text'>We encourage you to apply for other positions at {{CompanyName}} that may be a better fit for your background and experience. We also wish you the very best in your job search and future endeavors.</p>
            
                                        <div class='divider'></div>
                                        <p class='body-text' style='font-size: 13px; color: #888;'>Best regards,<br><strong>The {{CompanyName}} Team</strong></p>
                                    </div>
                                    <div class='footer'>
                                        <p>This is an automated message from {{CompanyName}}. Please do not reply to this email.</p>
                                    </div>
                                </div>
                            </body>
                            </html>
                                    ",
                    UsersId = employerId,
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true
                });
            }

            if (!existingTemplates.Contains("Hired"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "Hired",
                    Subject = "Congratulations — You're hired!",
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
                                    background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%);
                                    color: #333;
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
                                    background-color: #fff8e1;
                                    border-left: 4px solid #ffc107;
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
                                    color: #ff9800;
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
                                .cta-button {
                                    display: inline-block;
                                    background-color: #ff9800;
                                    color: white;
                                    padding: 12px 30px;
                                    text-decoration: none;
                                    border-radius: 4px;
                                    font-weight: 600;
                                    margin: 20px 0;
                                    font-size: 14px;
                                }
                                .cta-button:hover {
                                    background-color: #f57c00;
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
                                    <h1>🎉 Welcome Aboard!</h1>
                                </div>
                                <div class='content'>
                                    <p class='greeting'>Dear {{ApplicantName}},</p>
                                    <p class='body-text'>We're thrilled to offer you the position of <strong>{{JobTitle}}</strong> at <strong>{{CompanyName}}</strong>. Your background, skills, and enthusiasm impressed us, and we're confident you'll be a great addition to our team.</p>
            
                                    <div class='highlight-box'>
                                        <p class='highlight-label'>Position Details</p>
                                        <p class='highlight-value'>{{JobTitle}}</p>
                                        <p style='margin-top: 15px; font-size: 13px; color: #666;'>
                                            <strong>Company:</strong> {{CompanyName}}<br>
                                            <strong>Next Steps:</strong> We'll be in touch soon with details about your start date and onboarding process.
                                        </p>
                                    </div>
            
                                    <p class='body-text'>We look forward to working with you and are excited about the opportunities ahead. If you have any questions in the meantime, please don't hesitate to reach out.</p>
            
                                    <div style='text-align: center;'>
                                        <a href='#' class='cta-button'>View Your Offer Details</a>
                                    </div>
            
                                    <div class='divider'></div>
                                    <p class='body-text' style='font-size: 13px; color: #888;'>Best regards,<br><strong>The {{CompanyName}} Team</strong></p>
                                </div>
                                <div class='footer'>
                                    <p>This is an automated message from {{CompanyName}}. Please do not reply to this email.</p>
                                </div>
                            </div>
                        </body>
                        </html>
                                ",
                    UsersId = employerId,
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true
                });
            }

            if (templates.Any())
            {
                _context.EmailTemplate.AddRange(templates);
                _context.SaveChanges();
            }
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
                    .Where(u => (u.isDeleted == true) && (u.JobsId != null || u.TrainingId != null) && (u.Jobs.isFinal == null && u.Training.isFinal == null)).OrderByDescending(u => u.DateReported).Distinct().ToList();
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
                var isJobs = await _context.PostReport.Where(r => r.JobsId == report.JobsId).ToListAsync();
                var isTraining = await _context.PostReport.Where(r => r.TrainingId == report.TrainingId).ToListAsync();
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
                var isJobs = await _context.PostReport.Where(r => r.JobsId == report.JobsId).ToListAsync();
                var isTraining = await _context.PostReport.Where(r => r.TrainingId == report.TrainingId).ToListAsync();

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
        public async Task<IActionResult> ApplicationApproval(int SubscriptionId, int Id, string ApprovalDetails, string? DeclineReason)
        {
            try
            {
                var employer = _context.EmployerDetails.Include(u => u.Users).Where(u => u.UsersId == Id).FirstOrDefault();

                if (ApprovalDetails == "decline" && DeclineReason != null)
                {
                    var data = _context.Subscription.Where(e => e.Id == SubscriptionId).FirstOrDefault();

                    // Load decline email template
                    string emailBody = LoadTemplate("Declined.html");

                    // Send email
                    await _email.SendEmailAsync(employer.Users.Email, "Application Declined", emailBody);
                    data.Expiration = DateTime.Now;
                    data.Status = "Expired";
                    
                    _context.Subscription.Update(data);
                    employer.RejectionReason = DeclineReason;
                    employer.Status = "Decline";
                    _context.EmployerDetails.Update(employer);

                    await _context.SaveChangesAsync();

                    TempData["success"] = "Employer declined";
                    return RedirectToAction("Index");
                }
                else if (ApprovalDetails == "approve")
                {
                    var data = _context.Subscription.Where(e => e.Id == SubscriptionId).FirstOrDefault();
                    // Load approval template
                    string emailBody = LoadTemplate("Approved.html");

                    // Send email
                    await _email.SendEmailAsync(employer.Users.Email, "Employer Approved!", emailBody);
                    data.Expiration = null;
                    data.Status = "Current";
                    _context.Subscription.Update(data);

                    employer.Status = "Approved";
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
