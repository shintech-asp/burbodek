using burbodek.Data;
using burbodek.Migrations;
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
                data.Status = "Expired";
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

                data.Expiration = null;
                data.Status = "Current";
                _context.Subscription.Update(data);
                var employer = _context.EmployerDetails.Where(u => u.UsersId == Id).FirstOrDefault();
                employer.Status = "Approved";
                _context.EmployerDetails.Update(employer);
                _context.SaveChanges();
                AddDefaultEmailTemplates(Id);
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
