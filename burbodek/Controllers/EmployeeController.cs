using burbodek.Data;
using burbodek.Models;
using burbodek.Models.ViewModels;
using burbodek.Services;
using Microsoft.AspNetCore.Authorization;
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
        public IActionResult Index(string keyword, string location, int page = 1)
        {
            int pageSize = 10;
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

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
                    CreatedAt = j.CreatedAt,
                    AlreadyApplied = j.JobApplication.Any(a => a.AppliedBy == userId)
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
                    CreatedAt = t.CreatedAt,
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
                Location = location
            };

            return View(viewModel);
        }
        public IActionResult MarkAsStarred(int Id)
        {
            if(Id != null)
            {
                var email = _context.Emails.FirstOrDefault(u => u.Id == Id);
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
            return View(data);
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
                        .Where(u => u.Id == Id && u.isArchived == null)
                        .FirstOrDefault();
            return View(data);
        }

        public IActionResult TrainingInfo(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Training
                        .Include(u => u.Users)
                            .ThenInclude(u => u.EmployerDetails)
                        .Include(u => u.TrainingRequirements)
                        .Include(u => u.TrainingMedia)
                        .Include(u => u.TrainingBenefits)
                        .Where(u => u.Id == Id && u.isArchived == null)
                        .FirstOrDefault();
            return View(data);
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
                .Where(r => r.Email.SenderID == currentUserId && !r.IsTrashed)
                .OrderByDescending(r => r.Email.SentAt)
                .Select(r => r.Email)
                .Distinct()
                .ToList();

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
                .Where(r => !r.IsTrashed && r.Email.IsStarred &&)
                .OrderByDescending(r => r.Email.SentAt)
                .Select(r => r.Email)
                .Distinct()
                .ToList();

            return View(inboxEmails);
        }
        public IActionResult Message()
        {
            int currentUserId = int.Parse(User.FindFirst("UsersId")?.Value); // however you identify logged-in user

            var inboxEmails = _context.EmailRecipients
                .Include(r => r.Email)
                    .ThenInclude(e => e.Recipients)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Thread)
                .Include(r => r.Email)
                    .ThenInclude(e => e.Sender)
                .Where(r => r.RecipientID == currentUserId && !r.IsTrashed)
                .OrderByDescending(r => r.Email.SentAt)
                .Select(r => r.Email)
                .Distinct()
                .ToList();

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

                // Get the thread with all emails
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

                return View(viewModel);
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
                    CreatedAt = DateTime.Now
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
                return RedirectToAction("Index");
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
        public async Task<IActionResult> SaveDraft(SendEmailViewModel model, List<IFormFile> files)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

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

        [HttpGet]
        public async Task<JsonResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            var searchTerm = query.Trim().ToLower();

            var users = await _context.Users
                .Where(u => u.Username.ToLower().Contains(searchTerm) ||
                            u.Email.ToLower().Contains(searchTerm))
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
                        .Where(u => u.Id == Id && u.isArchived == null)
                        .FirstOrDefault();

            var userInfo = _context.JobApplication.Where(a => a.AppliedBy == userId).OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefault();

            var viewModel = new JobApplyViewModel
            {
                Jobs = data,
                UserInfo = userInfo
            };
            return View(viewModel);
        }

        public IActionResult TrainingApply(int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var data = _context.Training
                        .Include(u => u.Users)
                            .ThenInclude(u => u.EmployerDetails)
                        .Include(u => u.TrainingRequirements)
                        .Include(u => u.TrainingMedia)
                        .Include(u => u.TrainingBenefits)
                        .Where(u => u.Id == Id && u.isArchived == null)
                        .FirstOrDefault();
            var userInfo = _context.TrainingApplication.Where(a => a.AppliedBy == userId).OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefault();

            var viewModel = new TrainingApplyViewModel
            {
                Training = data,
                UserInfo = userInfo
            };
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrainingApply(TrainingApplication model, int Id, IFormFile? ResumeFile, IFormFile? CoeFile, IFormFile? TorFile, IFormFile? SeamansBookFile, IFormFile? PassportIdFile, IFormFile? DiplomaFile)
        {
            // Remove unrelated properties from ModelState
            ModelState.Remove("Jobs");
            ModelState.Remove("AppliedBy");
            ModelState.Remove("CV");
            ModelState.Remove("Training");

            model.TrainingId = Id;
            model.AppliedBy = int.Parse(User.FindFirst("UsersId")?.Value ?? throw new InvalidOperationException("User ID not found"));

            // Debug: Log all received files
            Console.WriteLine($"ResumeFile: {(ResumeFile != null ? ResumeFile.FileName : "null")}");
            Console.WriteLine($"CoeFile: {(CoeFile != null ? CoeFile.FileName : "null")}");
            Console.WriteLine($"TorFile: {(TorFile != null ? TorFile.FileName : "null")}");
            Console.WriteLine($"SeamansBookFile: {(SeamansBookFile != null ? SeamansBookFile.FileName : "null")}");
            Console.WriteLine($"PassportIdFile: {(PassportIdFile != null ? PassportIdFile.FileName : "null")}");
            Console.WriteLine($"DiplomaFile: {(DiplomaFile != null ? DiplomaFile.FileName : "null")}");

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
                PaymentStatus = model.PaymentStatus,
                Resume = await SaveFile(ResumeFile),
                Diploma = await SaveFile(DiplomaFile),
                PassportId = await SaveFile(PassportIdFile),
                Tor = await SaveFile(TorFile),
                Coe = await SaveFile(CoeFile),
                SeamansBook = await SaveFile(SeamansBookFile)
            };
            _context.TrainingApplication.Add(trainingApplication);
            await _context.SaveChangesAsync();

            var trainingDesc = _context.Training.FirstOrDefault(u => u.Id == model.TrainingId);
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
            TempData["success"] = "Application submitted successfully!";
            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JobApply(JobApplication model, int Id, IFormFile? ResumeFile, IFormFile? CoeFile, IFormFile? TorFile, IFormFile? SeamansBookFile, IFormFile? PassportIdFile, IFormFile? DiplomaFile)
        {
            // Remove unrelated properties from ModelState
            ModelState.Remove("Jobs");
            ModelState.Remove("AppliedBy");
            ModelState.Remove("CV");

            model.JobsId = Id;
            model.AppliedBy = int.Parse(User.FindFirst("UsersId")?.Value ?? throw new InvalidOperationException("User ID not found"));

            // Debug: Log all received files
            Console.WriteLine($"ResumeFile: {(ResumeFile != null ? ResumeFile.FileName : "null")}");
            Console.WriteLine($"CoeFile: {(CoeFile != null ? CoeFile.FileName : "null")}");
            Console.WriteLine($"TorFile: {(TorFile != null ? TorFile.FileName : "null")}");
            Console.WriteLine($"SeamansBookFile: {(SeamansBookFile != null ? SeamansBookFile.FileName : "null")}");
            Console.WriteLine($"PassportIdFile: {(PassportIdFile != null ? PassportIdFile.FileName : "null")}");
            Console.WriteLine($"DiplomaFile: {(DiplomaFile != null ? DiplomaFile.FileName : "null")}");

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
                Experience = model.Experience,
                ApplicationLetter = model.ApplicationLetter,
                Resume = await SaveFile(ResumeFile),
                Diploma = await SaveFile(DiplomaFile),
                PassportId = await SaveFile(PassportIdFile),
                Tor = await SaveFile(TorFile),
                Coe = await SaveFile(CoeFile),
                SeamansBook = await SaveFile(SeamansBookFile)
            };

            _context.JobApplication.Add(jobApplication);
            await _context.SaveChangesAsync();

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
                       .Where(u => u.Id == Id && u.isArchived == null)
                       .FirstOrDefault();

            if (data == null)
            {
                return NotFound(); // or redirect to an error page
            }

            return View(data);
        }
        public IActionResult AccountSettings()
        {
            return View();
        }
    }
}
