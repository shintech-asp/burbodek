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
using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController : Controller
    {
        ApplicationDbContext _context;
        private readonly IPaymongo _paymongo;
        private readonly IWebHostEnvironment _environment;
        public EmployerController(ApplicationDbContext context, IPaymongo paymongo, IWebHostEnvironment environment)
        {
            _context = context;
            _paymongo = paymongo;
            _environment = environment;
        }
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var data = _context.Subscription.Where(u => u.UsersId == userId && (u.Expiration > DateTime.Now || !u.Expiration.HasValue) && u.Status == "Current").FirstOrDefault();
            var jobs = _context.Jobs.Include(u => u.JobApplication).Where(u => u.UsersId == userId).ToList();
            var training = _context.Training.Include(u => u.TrainingApplication).Where(u => u.UsersId == userId).ToList();
            var campaigns = _context.Campaign.Where(u => u.CreatedByUserId == userId).ToList();
            var userPaymentOption = _context.PaymentDetails.Where(u => u.UsersId == userId).ToList();

            var dashboard = new EmployerCampaignViewModel
            {
                Subscription = data,
                Jobs = jobs,
                Training = training,
                Campaign = campaigns,
                PaymentDetails = userPaymentOption
            };
            return View(dashboard);
        }
        public IActionResult ActivateCampaing(int id)
        {
            var campaing = _context.Campaign.Find(id);
            if (campaing != null)
            {
                campaing.IsActive = true;
                _context.Campaign.Update(campaing);
                _context.SaveChanges();
                return Json(new { success = true, message = "Campaign Activated successfully!" });
            }
            return Json(new { success = false, message = "Error occured!" });

        }
        public IActionResult DeactivateCampaing(int id)
        {
            var campaing = _context.Campaign.Find(id);
            if(campaing != null)
            {
                campaing.IsActive = false;
                _context.Campaign.Update(campaing);
                _context.SaveChanges();
                return Json(new { success = true, message = "Campaign deactivated successfully!" });
            }
            return Json(new { success = false, message = "Error occured!" });

        }
        [HttpGet]
        public IActionResult GetCampaign(int id)
        {
            var campaign = _context.Campaign
                .Include(c => c.CreatedByUser)
                .FirstOrDefault(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            string listingTitle = null;

            if (campaign.ListingType == "Jobs")
            {
                listingTitle = _context.Jobs
                    .Where(j => j.Id == campaign.SelectedListingId)
                    .Select(j => j.JobTitle)
                    .FirstOrDefault();
            }
            else if (campaign.ListingType == "Training")
            {
                listingTitle = _context.Training
                    .Where(t => t.Id == campaign.SelectedListingId)
                    .Select(t => t.Name)
                    .FirstOrDefault();
            }

            return Json(new
            {
                campaign.Id,
                campaign.CampaignName,
                campaign.CampaignDescription,
                campaign.LogoFilePath,
                campaign.City,
                campaign.Province,
                campaign.FullAddress,
                campaign.CreatedAt,
                campaign.IsActive,
                campaign.ListingType,
                ListingName = listingTitle,
                CreatorName = campaign.CreatedByUser?.Username
            });
        }


        [HttpPost]
        public async Task<IActionResult> SubmitCampaign(Campaign campaign, IFormFile CampaignLogo)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var userPlan = User.FindFirst("Plan")?.Value;
            ModelState.Remove("CreatedByUser");
            ModelState.Remove("SelectedTraining");
            ModelState.Remove("SelectedJob");
            ModelState.Remove("LogoFilePath");
            if (!ModelState.IsValid)
                return BadRequest("Invalid campaign data.");
            if(userPlan == "Basic" && campaign.PaymentDetailsId == null)
            {
                return Json(new { success = false, message = "Error: Please add a payment details "});
            }
            try
            {
                if(campaign.PaymentDetailsId != null)
                {
                    // ✅ Handle File Upload
                    if (CampaignLogo != null && CampaignLogo.Length > 0)
                    {
                        // Create upload folder if not existing
                        string uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "campaigns");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        // Generate unique filename
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(CampaignLogo.FileName);
                        string filePath = Path.Combine(uploadPath, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await CampaignLogo.CopyToAsync(fileStream);
                        }

                        // Save relative path for database
                        campaign.LogoFilePath = Path.Combine("/uploads/campaigns/", uniqueFileName);
                    }

                    // ✅ Save campaign to database
                    campaign.CreatedAt = DateTime.Now;
                    campaign.IsActive = false; // You can set to "Active" if needed
                    campaign.CreatedByUserId = userId;
                    campaign.Payment = (decimal)(_context.Plans.FirstOrDefault(u => u.Id == 2).Price/10);
                    campaign.isPaid = false;
                    _context.Campaign.Add(campaign);
                    await _context.SaveChangesAsync();

                    // ✅ Return success
                    TempData["campaignId"] = campaign.Id;
                    return Json(new
                    {
                        success = true,
                        redirectUrl = Url.Action("CheckoutCampaign", new { id = campaign.Id })
                    });

                }
                else
                {
                    // ✅ Handle File Upload
                    if (CampaignLogo != null && CampaignLogo.Length > 0)
                    {
                        // Create upload folder if not existing
                        string uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "campaigns");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        // Generate unique filename
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(CampaignLogo.FileName);
                        string filePath = Path.Combine(uploadPath, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await CampaignLogo.CopyToAsync(fileStream);
                        }

                        // Save relative path for database
                        campaign.LogoFilePath = Path.Combine("/uploads/campaigns/", uniqueFileName);
                    }

                    // ✅ Save campaign to database
                    campaign.CreatedAt = DateTime.Now;
                    campaign.IsActive = true; // You can set to "Active" if needed
                    campaign.CreatedByUserId = userId;
                    _context.Campaign.Add(campaign);
                    await _context.SaveChangesAsync();

                    // ✅ Return success
                    return Json(new { success = true, message = "Campaign submitted successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
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

        public async Task<IActionResult> SuccessCampaignPayment()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            // Example: values you might pass via TempData or querystring
            var campaign = TempData["Campaign"] != null ? Convert.ToInt32(TempData["Campaign"]) : 0;
            int amount = TempData["Amount"] != null ? Convert.ToInt32(TempData["Amount"]) : 0;
            var data = _context.Campaign.Find(campaign);
            data.isPaid = true;
            data.IsActive = true;
            _context.Campaign.Update(data);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> CheckoutCampaign(int Id)
        {
            try
            {

                // ✅ Basic validation
                if (Id <= 0 )
                {
                    // Return back with validation error
                    TempData["Error"] = "Invalid checkout details. Please make sure all fields are filled correctly.";
                    return RedirectToAction("Index", "Employer");
                }
                var campaign = _context.Campaign.FirstOrDefault(u => u.Id == Id);
                var userDetails = _context.Users.Find(campaign.CreatedByUserId);
                var paymentDetails = _context.PaymentDetails.FirstOrDefault(u => u.Id == campaign.PaymentDetailsId);
                // ✅ Create checkout session
                var responseJson = await _paymongo.CreateCheckoutCampaignSession(
                    campaign.Payment,
                    "PHP",
                    paymentDetails.Name,
                    userDetails.Email,
                    paymentDetails.PhoneNumber,
                    "Campaign"
                );

                // ✅ Parse response
                var json = JObject.Parse(responseJson);
                var checkoutUrl = json["data"]?["attributes"]?["checkout_url"]?.ToString();

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    TempData["Error"] = "Failed to retrieve checkout URL. Please try again later.";
                    return RedirectToAction("Subscription", "Employer");
                }

                TempData["Campaign"] = campaign.Id;
                TempData["Amount"] = (int)campaign.Payment;

                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Checkout failed: {ex.Message}";
                return RedirectToAction("Index", "Employer");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Checkout(decimal amount, string planName, string email, string? contact, string username)
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
        [HttpPost]
        public async Task<IActionResult> UploadCertificate(IFormFile file, int trainingApplicationId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "certificates");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var trainingCertificate = new TrainingCertificate
                {
                    FilePath = $"/uploads/certificates/{fileName}",
                    FileType = file.ContentType,
                    TrainingApplicationId = trainingApplicationId
                };
                _context.TrainingCertificate.Add(trainingCertificate);
                await _context.SaveChangesAsync();

                // ========== SEND EMAIL NOTIFICATION ==========
                try
                {
                    var trainingApplication = await _context.TrainingApplication
                        .Include(ta => ta.Training)
                        .ThenInclude(t => t.Users)
                        .ThenInclude(u => u.EmployerDetails)
                        .FirstOrDefaultAsync(ta => ta.Id == trainingApplicationId);

                    if (trainingApplication != null)
                    {
                        var applicant = await _context.Users.FirstOrDefaultAsync(u => u.Id == trainingApplication.AppliedBy);

                        if (applicant != null)
                        {
                            // Create email thread
                            var sendEmail = new EmailThread
                            {
                                Subject = "Your Certificate Has Been Uploaded - " + trainingApplication.Training.Name,
                                CreatedBy = trainingApplication.Training.UsersId,
                                CreatedAt = DateTime.Now
                            };
                            _context.EmailThreads.Add(sendEmail);
                            await _context.SaveChangesAsync();

                            var email = new Email
                            {
                                Thread = sendEmail,
                                SenderID = trainingApplication.Training.UsersId,
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
        .certificate-box {
            background-color: #f0f7ff;
            border-left: 4px solid #0066cc;
            padding: 20px;
            margin: 20px 0;
            border-radius: 4px;
            text-align: center;
        }
        .certificate-icon {
            font-size: 48px;
            margin-bottom: 10px;
        }
        .certificate-text {
            font-weight: 600;
            color: #0066cc;
            font-size: 16px;
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
            text-align: center;
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
            <h1>🎓 Certificate Uploaded!</h1>
        </div>
        <div class='content'>
            <p class='greeting'>Dear " + applicant.Username + @",</p>
            <p class='body-text'>Congratulations! Your certificate for the training program has been successfully uploaded and is now available in your account.</p>

            <div class='highlight-box'>
                <p class='highlight-label'>Training Program</p>
                <p class='highlight-value'>" + trainingApplication.Training.Name + @"</p>
                <p style='margin-top: 15px; font-size: 13px; color: #666;'>
                    <strong>Provider:</strong> " + trainingApplication.Training.Users?.EmployerDetails?.BusinessName + @"
                </p>
            </div>

            <div class='certificate-box'>
                <div class='certificate-icon'>📜</div>
                <div class='certificate-text'>Your certificate is ready for download!</div>
            </div>

            <p class='body-text'>You can now download your certificate and add it to your professional credentials. This certification recognizes your successful completion of the training program.</p>

            <p class='body-text'>Thank you for completing this training program with us. We hope you found it valuable and informative. If you have any questions or need assistance, please don't hesitate to reach out.</p>

            <div style='text-align: center;'>
                <p style='font-size: 13px; color: #888;'>Log in to your account to view and download your certificate.</p>
            </div>

            <div class='divider'></div>
            <p class='body-text' style='font-size: 13px; color: #888;'>Best regards,<br><strong>The " + trainingApplication.Training.Users?.EmployerDetails?.BusinessName + @" Team</strong></p>
        </div>
        <div class='footer'>
            <p>This is an automated message from " + trainingApplication.Training.Users?.EmployerDetails?.BusinessName + @". Please do not reply to this email.</p>
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
                                RecipientID = trainingApplication.AppliedBy,
                                RecipientType = RecipientType.TO,
                                IsRead = false,
                                IsTrashed = false,
                                IsStarred = false
                            };
                            _context.EmailRecipients.Add(emailRecipient);
                            await _context.SaveChangesAsync();

                            Console.WriteLine($"Certificate upload notification sent to {applicant.Email}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending certificate notification email: {ex.Message}");
                    // Don't fail the certificate upload if email fails
                }

                return Json(new { success = true, message = "File uploaded successfully! Applicant has been notified." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading certificate: {ex.Message}");
                return Json(new { success = false, message = "Error uploading file: " + ex.Message });
            }
        }

        public IActionResult JobDetails(int Id)
        {
            var data = _context.Jobs
                        .Include(u => u.Users)
                            .ThenInclude(u => u.EmployerDetails)
                        .Include(u => u.JobBenefits)
                        .Include(u => u.JobApplication)
                        .Include(u => u.JobRequirements)
                        .Include(u => u.JobRole)
                        .Include(u => u.JobMedia)
                        .FirstOrDefault(u => u.Id == Id && u.isArchived == null);

            if (data == null)
            {
                return NotFound(); // or redirect to an error page
            }

            return View(data);
        }
        public IActionResult SetAsPaid(int Id)
        {
            var Application = _context.TrainingApplication.Where(u => u.Id == Id).FirstOrDefault();
            Application.PaymentStatus = "Paid";
            var Payment = _context.TrainingPayments.Where(u => u.TrainingApplicationId == Id).FirstOrDefault();
            Payment.Paid = Payment.Price;
            Payment.PaymentOption = "Full";
            _context.TrainingApplication.Update(Application);
            _context.TrainingPayments.Update(Payment);
            _context.SaveChanges();

            return Json(new { response = true });
        }
        public IActionResult TrainingDetails(int Id)
        {
            // Load training with all necessary relationships
            var data = _context.Training
                .Include(u => u.Users)
                    .ThenInclude(u => u.EmployerDetails)
                .Include(u => u.TrainingBenefits)
                .Include(u => u.TrainingRequirements)
                .Include(u => u.TrainingMedia)
                .Include(u => u.TrainingBadge)
                .Include(u => u.TrainingApplication)
                    .ThenInclude(u => u.TrainingPayments)
                .FirstOrDefault(u => u.Id == Id && u.isArchived == null);

            if (data == null)
            {
                return NotFound(); // Training not found
            }

            // Safely get payment info if it exists
            var payment = _context.TrainingPayments
                .FirstOrDefault(u => u.TrainingApplication.TrainingId == Id);

            // Determine payment status / mode
            if (payment != null)
            {
                if (payment.Price == payment.Paid)
                {
                    ViewBag.PaymentStatus = "Fully Paid";
                }
                else if (payment.Paid > 0 && payment.Paid < payment.Price)
                {
                    ViewBag.PaymentStatus = "Partially Paid (Down Payment)";
                }
                else
                {
                    ViewBag.PaymentStatus = "Slot reserved";
                }

                // Mode of payment
                ViewBag.ModeOfPayment = payment.PaymentOption switch
                {
                    "Full" => "Full Payment Required",
                    "Down" => "Down Payment Allowed",
                    _ => "No Payment Required"
                };
            }
            else
            {
                ViewBag.PaymentStatus = "No Payment Required";
                ViewBag.ModeOfPayment = "None";
            }

            return View(data);
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
        public IActionResult getJobApplicants(int id)
        {
            var data = _context.JobApplication
                .Where(u => u.JobsId == id)
                .Select(u => new {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.MobileNo,
                    u.ExpectedSalary,
                    u.Resume,
                    u.SeamansBook,
                    u.Diploma,
                    u.Coe,
                    u.Tor,
                    u.PassportId,
                    u.ApplicationLetter,
                    u.City,
                    u.Age,
                    u.Status
                })
                .ToList();

            return Json(new { count = data.Count, data = data });
        }

        public IActionResult GetTraining()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var trainings = _context.Training
                .Where(t => t.UsersId == userId && t.isArchived == null)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Expiration,
                    ApplicantsCount = t.TrainingApplication.Count()
                })
                .ToList();

            return Json(new { response = trainings });
        }

        public IActionResult GetJobs()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var jobs = _context.Jobs
                .Where(j => j.UsersId == userId && j.isArchived == null)
                .Select(j => new
                {
                    j.Id,
                    j.JobTitle,
                    j.JobType,
                    j.ExpirationDate,
                    ApplicantsCount = j.JobApplication.Count()
                })
                .ToList();

            return Json(new { response = jobs });
        }
        public async Task<IActionResult> AddStartDateTraining(DateTime startDate, int TrainingId, string applicationIds = "")
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UsersId")?.Value);

                var training = _context.Training
                    .Include(t => t.Users)
                    .ThenInclude(u => u.EmployerDetails)
                    .Where(j => j.UsersId == userId && j.Id == TrainingId)
                    .FirstOrDefault();

                if (training == null)
                {
                    return Json(new { response = false, message = "Training not found" });
                }

                // Parse duration safely
                int trainingDays = 0;
                if (!string.IsNullOrEmpty(training.Duration) && int.TryParse(training.Duration, out int days))
                {
                    trainingDays = days;
                }

                // Parse the comma-separated application IDs
                var applicationIdList = string.IsNullOrEmpty(applicationIds)
                    ? new List<int>()
                    : applicationIds.Split(',').Select(id => int.TryParse(id.Trim(), out var result) ? result : 0)
                        .Where(id => id > 0).ToList();

                // If no specific IDs provided, get all applicants for this training
                if (applicationIdList.Count == 0)
                {
                    applicationIdList = _context.TrainingApplication
                        .Where(ta => ta.TrainingId == TrainingId)
                        .Select(ta => ta.AppliedBy)
                        .ToList();
                }

                // Send emails to each applicant
                foreach (var applicantId in applicationIdList)
                {
                    try
                    {
                        var applicantInfo = _context.Users.FirstOrDefault(u => u.Id == applicantId);

                        if (applicantInfo == null) continue;

                        // Calculate end date
                        DateTime endDate = startDate.AddDays(trainingDays);

                        // Create email thread
                        var sendEmail = new EmailThread
                        {
                            Subject = "Training Start Date Announced - " + training.Name,
                            CreatedBy = training.UsersId,
                            CreatedAt = DateTime.Now
                        };
                        _context.EmailThreads.Add(sendEmail);
                        await _context.SaveChangesAsync();

                        var email = new Email
                        {
                            Thread = sendEmail,
                            SenderID = training.UsersId,
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
                                            .date-box {
                                                background-color: #fff3e0;
                                                border-left: 4px solid #ff9800;
                                                padding: 15px;
                                                margin: 15px 0;
                                                border-radius: 4px;
                                            }
                                            .date-item {
                                                display: flex;
                                                justify-content: space-between;
                                                padding: 10px 0;
                                                font-size: 14px;
                                            }
                                            .date-item strong {
                                                color: #ff9800;
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
                                                <h1>📅 Training Start Date Announced!</h1>
                                            </div>
                                            <div class='content'>
                                                <p class='greeting'>Dear " + applicantInfo.Username + @",</p>
                                                <p class='body-text'>Great news! We're excited to inform you that the start date for your training program has been confirmed.</p>
            
                                                <div class='highlight-box'>
                                                    <p class='highlight-label'>Training Details</p>
                                                    <p class='highlight-value'>" + training.Name + @"</p>
                                                    <p style='margin-top: 15px; font-size: 13px; color: #666;'>
                                                        <strong>Training Provider:</strong> " + training.Users?.EmployerDetails?.BusinessName + @"
                                                    </p>
                                                </div>
            
                                                <div class='date-box'>
                                                    <div class='date-item'>
                                                        <strong>📌 Start Date:</strong>
                                                        <span>" + startDate.ToString("MMMM dd, yyyy") + @"</span>
                                                    </div>
                                                    <div class='date-item'>
                                                        <strong>📌 End Date:</strong>
                                                        <span>" + endDate.ToString("MMMM dd, yyyy") + @"</span>
                                                    </div>
                                                    <div class='date-item'>
                                                        <strong>⏱️ Duration:</strong>
                                                        <span>" + trainingDays + @" days</span>
                                                    </div>
                                                </div>
            
                                                <p class='body-text'>Please make sure you're prepared for the training on the scheduled date. If you have any questions or concerns, don't hesitate to reach out to us.</p>
            
                                                <div class='divider'></div>
                                                <p class='body-text' style='font-size: 13px; color: #888;'>Best regards,<br><strong>The " + training.Users?.EmployerDetails?.BusinessName + @" Team</strong></p>
                                            </div>
                                            <div class='footer'>
                                                <p>This is an automated message from " + training.Users?.EmployerDetails?.BusinessName + @". Please do not reply to this email.</p>
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
                            RecipientID = applicantId,
                            RecipientType = RecipientType.TO,
                            IsRead = false,
                            IsTrashed = false,
                            IsStarred = false
                        };
                        _context.EmailRecipients.Add(emailRecipient);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending email to applicant {applicantId}: {ex.Message}");
                    }
                }

                // Update training with start and end dates
                training.StartDate = startDate;
                training.EndDate = startDate.AddDays(trainingDays);
                _context.Training.Update(training);
                await _context.SaveChangesAsync();

                return Json(new { response = true, message = $"Training start date set and {applicationIdList.Count} applicants notified!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { response = false, message = ex.Message });
            }
        }
        public IActionResult JobListing()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            _context.Database.SetCommandTimeout(120);
            var data = _context.Jobs
                        .Include(u => u.JobBenefits)
                        .Include(u => u.JobApplication)
                        .Include(u => u.JobRequirements)
                        .Include(u => u.JobRole)
                        .Include(u => u.JobMedia)
                        .Where(u => u.UsersId == userId && u.isArchived == null)
                        .ToList();
            var training = _context.Training
                        .Include(u => u.TrainingBenefits)
                        .Include(u => u.TrainingRequirements)
                        .Include(u => u.TrainingMedia)
                        .Where(u => u.UsersId == userId && u.isArchived == null)
                        .ToList();

            var jobs = _context.JobApplication.FirstOrDefault();
                    

            var jobList = new JobListingViewModel
            {
                JobsList = data,
                JobApplication = jobs,
                TrainingList = training
            };
            return View(jobList);
        }
        public IActionResult AccountSettings()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var userData = _context.Users.Include(u => u.EmployerDetails).Include(u => u.Jobs).Include(u => u.Training).FirstOrDefault(u => u.Id == userId);
            var TotalApplication = _context.TrainingApplication
                                .Include(u => u.Training)
                                    .ThenInclude(u => u.Users)
                                .Include(u => u.TrainingPayments)
                                .Where(u => u.Training.UsersId == userId).ToList();
            var TotalJobApplication = _context.JobApplication.Where(u => u.Jobs.UsersId == userId).ToList();
            decimal? total = 0;
            int? totalSuccessApplicant = 0;
            totalSuccessApplicant += TotalApplication.Where(u => u.PaymentStatus == "Paid").Count();
            totalSuccessApplicant += TotalJobApplication.Where(u => u.Status == "Hired").Count();
            foreach (var data in TotalApplication)
            {
                foreach (var sumData in data.TrainingPayments)
                {
                    total += sumData.Paid ?? 0;
                    ViewBag.TotalCash = total;
                }
            }
            ViewBag.TotalHired = totalSuccessApplicant;
            return View(userData);
        }

        public IActionResult ChangeProfileDetails(string FullName, string BusinessName, string Address)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var user = _context.Users
                        .Include(u => u.EmployerDetails) // Make sure EmployerDetails is loaded
                        .FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return Json(new { response = false, message = "User not found." });

            user.Username = FullName ?? user.Username;
            user.EmployerDetails.BusinessName = BusinessName ?? user.EmployerDetails.BusinessName;
            user.EmployerDetails.Address = Address ?? user.EmployerDetails.Address;

            _context.Users.Update(user);
            _context.SaveChanges();

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

        public IActionResult AccountEmail()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var userData = _context.Users
                .Include(u => u.EmployerDetails)
                .Include(u => u.Jobs)
                .Include(u => u.Training)
                .Include(u => u.EmailTemplate)
                .FirstOrDefault(u => u.Id == userId);

            // Initialize default email templates if they don't exist
            var existingTemplates = userData.EmailTemplate?.Select(t => t.TypeOfEmail).ToList() ?? new List<string>();
            var templates = new List<EmailTemplate>();

            if (!existingTemplates.Contains("Applied"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "Applied",
                    Subject = "Your job application has been received!",
                    Body = @"<!DOCTYPE html>
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
            <p class='greeting'>Dear {{ApplicantName}},</p>
            <p class='body-text'>Thank you for applying to the <strong>{{JobTitle}}</strong> position at <strong>{{CompanyName}}</strong>. We've received your application and appreciate your interest in joining our team.</p>
            <div class='highlight-box'>
                <p class='body-text' style='margin: 0; font-size: 14px;'><strong>What's Next?</strong><br>Our hiring team is currently reviewing applications. If your qualifications match what we're looking for, we'll reach out to schedule an interview.</p>
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
</html>",
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true,
                    UsersId = userId
                });
            }

            if (!existingTemplates.Contains("For Interview"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "For Interview",
                    Subject = "Interview Invitation for {{JobTitle}}",
                    Body = @"<!DOCTYPE html>
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
                <p style='margin: 0; font-size: 14px; color: #333;'><strong>Interview Details</strong><br>Position: {{JobTitle}}<br><br>Our hiring team will contact you shortly with specific details about the interview format, date, and time. Please ensure your contact information is up to date.</p>
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
</html>",
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true,
                    UsersId = userId
                });
            }

            if (!existingTemplates.Contains("Rejected"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "Rejected",
                    Subject = "Update on your {{JobTitle}} application",
                    Body = @"<!DOCTYPE html>
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
</html>",
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true,
                    UsersId = userId
                });
            }

            if (!existingTemplates.Contains("Hired"))
            {
                templates.Add(new EmailTemplate
                {
                    Category = "Job Notification",
                    TypeOfEmail = "Hired",
                    Subject = "Congratulations — You're hired!",
                    Body = @"<!DOCTYPE html>
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
                <p style='margin: 0; font-size: 12px; font-weight: 600; color: #ff9800; text-transform: uppercase; letter-spacing: 0.5px;'>Position Details</p>
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
</html>",
                    ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = true,
                    UsersId = userId
                });
            }

            // Add new templates to database if they don't exist
            if (templates.Count > 0)
            {
                _context.EmailTemplate.AddRange(templates);
                _context.SaveChanges();

                // Reload user data with new templates
                userData = _context.Users
                    .Include(u => u.EmployerDetails)
                    .Include(u => u.Jobs)
                    .Include(u => u.Training)
                    .Include(u => u.EmailTemplate)
                    .FirstOrDefault(u => u.Id == userId);
            }

            // Calculate statistics
            var TotalApplication = _context.TrainingApplication
                .Include(u => u.Training)
                    .ThenInclude(u => u.Users)
                .Include(u => u.TrainingPayments)
                .Where(u => u.Training.UsersId == userId)
                .ToList();

            var TotalJobApplication = _context.JobApplication
                .Where(u => u.Jobs.UsersId == userId)
                .ToList();

            decimal? total = 0;
            int? totalSuccessApplicant = 0;

            totalSuccessApplicant += TotalApplication.Where(u => u.PaymentStatus == "Paid").Count();
            totalSuccessApplicant += TotalJobApplication.Where(u => u.Status == "Hired").Count();

            foreach (var data in TotalApplication)
            {
                foreach (var sumData in data.TrainingPayments)
                {
                    total += sumData.Paid ?? 0;
                }
            }

            ViewBag.TotalCash = total;
            ViewBag.TotalHired = totalSuccessApplicant;

            return View(userData);
        }
        public IActionResult AccountOverview()
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            var userData = _context.Users.Include(u => u.EmployerDetails).Include(u => u.Jobs).Include(u =>u.Training).FirstOrDefault(u => u.Id == userId);
            var TotalApplication = _context.TrainingApplication
                                .Include(u => u.Training)
                                    .ThenInclude(u =>u.Users)
                                .Include(u => u.TrainingPayments)
                                .Where(u => u.Training.UsersId == userId).ToList();
            var TotalJobApplication = _context.JobApplication.Where(u => u.Jobs.UsersId == userId).ToList();
            decimal? total = 0;
            int? totalSuccessApplicant = 0;
            totalSuccessApplicant += TotalApplication.Where(u => u.PaymentStatus == "Paid").Count();
            totalSuccessApplicant += TotalJobApplication.Where(u => u.Status == "Hired").Count();
            foreach (var data in TotalApplication)
            {
                foreach(var sumData in data.TrainingPayments)
                {
                    total += sumData.Paid ?? 0;
                    ViewBag.TotalCash = total;
                }
            }
            ViewBag.TotalHired = totalSuccessApplicant;

            return View(userData);
        }
        public IActionResult TrainingReceipt(int Id, int AppliedId)
        {
            var user = _context.TrainingApplication
                .Include(u => u.TrainingCertificate)
                .Include(u => u.TrainingPayments.Where(u => u.UsersId == AppliedId))
                    .ThenInclude(u => u.Users)
                .Include(u => u.Training)
                    .ThenInclude(u => u.Users)
                .Where(u => u.Id == Id)
                .FirstOrDefault();

            if (user == null)
                return NotFound();
            ViewBag.ApplicationId = user.Id;
            return View(user);
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
        public IActionResult ApplicantInfo(int Id, int ApplicantId)
        {
            var data = _context.Jobs
                       .Include(u => u.Users)
                           .ThenInclude(u => u.EmployerDetails)
                       .Include(u => u.JobBenefits)
                       .Include(u => u.JobApplication.Where(a => a.AppliedBy == ApplicantId))
                       .Include(u => u.JobRequirements)
                       .Include(u => u.JobRole)
                       .Include(u => u.JobMedia)
                       .Include(u => u.JobUploads.Where(u => u.isActive))
                           .ThenInclude(u => u.ApplicantJobUpload.Where(u => u.UsersId == ApplicantId))
                       .Where(u => u.Id == Id && u.isArchived == null)
                       .FirstOrDefault();

            if (data == null)
            {
                return NotFound(); // or redirect to an error page
            }

            return View(data);
        }
        [HttpPost]
        public IActionResult ApplicantInfo(int ApplicantId, string Status, int Id)
        {
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);
            // Get the applicant for this job
            var getStatus = _context.JobApplication
                .FirstOrDefault(u => u.AppliedBy == ApplicantId && u.JobsId == Id);
            var getUser = _context.Users
                .Include(u => u.EmployerDetails)
                .FirstOrDefault(u => u.Id == userId);
            var getJob = _context.Jobs
                .Find(Id);
            if (getStatus == null)
            {
                TempData["error"] = "Applicant not found.";
                return RedirectToAction("JobDetails", new { Id });
            }
            var emailTemplate = _context.EmailTemplate
                                .Where(u => u.UsersId == userId && u.TypeOfEmail == Status && u.isActive == true)
                                .FirstOrDefault();
            var emailContent = emailTemplate.Body
                                .Replace("{{ApplicantName}}", getStatus.FirstName)
                                .Replace("{{CompanyName}}", getUser.EmployerDetails.BusinessName)
                                .Replace("{{JobTitle}}", getStatus.Jobs.JobTitle);
            
            var data = _context.Jobs
                       .Include(u => u.Users)
                           .ThenInclude(u => u.EmployerDetails)
                       .Include(u => u.JobBenefits)
                       .Include(u => u.JobApplication.Where(a => a.AppliedBy == ApplicantId))
                       .Include(u => u.JobRequirements)
                       .Include(u => u.JobRole)
                       .Include(u => u.JobMedia)
                       .FirstOrDefault(u => u.Id == Id && u.isArchived == null);

            if (getStatus.Status == Status)
            {
                TempData["success"] = "Data submitted. Nothing changed.";
            }
            else
            {
                getStatus.Status = Status;
                _context.Update(getStatus);
                _context.SaveChanges();
                TempData["success"] = "Applicant status updated successfully.";
                var sendEmail = new EmailThread
                {
                    Subject = emailTemplate.Subject,
                    CreatedBy = userId,
                    IsTrashed = false
                };
                _context.EmailThreads.Add(sendEmail);
                _context.SaveChanges();
                var sendEmailContent = new Email
                {
                    ThreadID = sendEmail.Id,
                    Body = emailContent,
                    SenderID = userId,
                    IsDraft = false,
                    IsTrashed = false,
                    IsRead = false,
                    IsStarred = false,
                };
                _context.Emails.Add(sendEmailContent);
                _context.SaveChanges();
                var emailRecipient = new EmailRecipient
                {
                    EmailID = sendEmailContent.Id,
                    RecipientID = getStatus.AppliedBy,
                    IsStarred = false,
                    IsTrashed = false,
                    IsRead = false,
                };
                _context.EmailRecipients.Add(emailRecipient);
                _context.SaveChanges();
            }

            return View(data);
        }

        public IActionResult JobEdit(int Id)
        {
            var data = _context.Jobs
                       .Include(u => u.Users)
                           .ThenInclude(u => u.EmployerDetails)
                       .Include(u => u.JobBenefits)
                       .Include(u => u.JobApplication)
                       .Include(u => u.JobRequirements)
                       .Include(u => u.JobRole)
                       .Include(u => u.JobMedia)
                       .FirstOrDefault(u => u.Id == Id && u.isArchived == null);

            if (data == null)
            {
                return NotFound(); // or redirect to an error page
            }

            return View(data);
        }
        [HttpPost]
        public IActionResult JobEdit(int Id, Jobs model, List<string> Role, List<string> Requirement, List<string> Benefit)
        {
            ModelState.Remove("Users");
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fill up all the details.";
                var data = _context.Jobs
                       .Include(u => u.Users)
                           .ThenInclude(u => u.EmployerDetails)
                       .Include(u => u.JobBenefits)
                       .Include(u => u.JobApplication)
                       .Include(u => u.JobRequirements)
                       .Include(u => u.JobRole)
                       .Include(u => u.JobMedia)
                       .FirstOrDefault(u => u.Id == Id && u.isArchived == null);

                if (data == null)
                {
                    return NotFound(); // or redirect to an error page
                }

                return View(data);
            }
            var submitEdit = _context.Jobs.Find(Id);
            submitEdit.JobTitle = model.JobTitle;
            submitEdit.JobType = model.JobType;
            submitEdit.SalaryMin = model.SalaryMin;
            submitEdit.SalaryMax = model.SalaryMax;
            submitEdit.ExpirationDate = model.ExpirationDate;
            submitEdit.JobDescription = model.JobDescription;
            var requirements = _context.JobRequirements.Where(u => u.JobsId == Id).ToList();
            foreach (var req in requirements)
            {
                _context.JobRequirements.Remove(req);
            }
            foreach (var requirement in Requirement ?? Enumerable.Empty<string>())
            {
                var JobRequirements = new JobRequirements
                {
                    JobsId = Id,
                    Requirement = requirement
                };
                _context.JobRequirements.Add(JobRequirements);
            }
            var benefits = _context.JobBenefits.Where(u => u.JobsId == Id).ToList();
            foreach (var ben in benefits)
            {
                _context.JobBenefits.Remove(ben);
            }
            foreach (var benefit in Benefit ?? Enumerable.Empty<string>())
            {
                var JobBenefits = new JobBenefits
                {
                    JobsId = Id,
                    Benefit = benefit
                };
                _context.JobBenefits.Add(JobBenefits);
            }
            var roles = _context.JobRole.Where(u => u.JobsId == Id).ToList();
            foreach (var rol in roles)
            {
                _context.JobRole.Remove(rol);
            }
            foreach (var role in Role ?? Enumerable.Empty<string>())
            {
                var JobRole = new JobRole
                {
                    JobsId = Id,
                    Role = role
                };
                _context.JobRole.Add(JobRole);
            }
            _context.Jobs.Update(submitEdit);
            _context.SaveChanges();
            TempData["success"] = "Job details successfully edited";
            return RedirectToAction("JobListing");
        }
        public IActionResult JobDelete(int Id)
        {
            var data = _context.Jobs.Find(Id);
            data.isArchived = DateTime.Now;
            _context.Jobs.Update(data);
            _context.SaveChanges();
            TempData["success"] = "Job successfully deleted!";
            return RedirectToAction("JobListing");
        }
        public IActionResult JobCreate()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> JobCreate(JobCreateViewModel model, List<string> Role, List<string> Requirement, List<string> Benefit)
        {

            ModelState.Keys
            .Where(k => k.StartsWith("Uploads"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
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
                JobDescription = model.JobDescription,
                Diploma = false,
                Resume = false,
                Tor = false,
                Coe = false,
                SeamansBook = false,
                PassportId = false
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            foreach (var data in model.Uploads)
            {
                var jobUploads = new JobUploads
                {
                    JobsId = job.Id,
                    Name = data.Name,
                    isActive = data.isActive
                };

                _context.JobUploads.Add(jobUploads);
                await _context.SaveChangesAsync();
            }
            // Requirements (many)
            foreach (var requirement in Requirement ?? Enumerable.Empty<string>())
            {
                var JobRequirements = new JobRequirements
                {
                    JobsId = job.Id,
                    Requirement = requirement
                };
                _context.JobRequirements.Add(JobRequirements);
            }
            // Roles (many)
            foreach (var role in Role ?? Enumerable.Empty<string>())
            {
                var JobRole = new JobRole
                {
                    JobsId = job.Id,
                    Role = role
                };
                _context.JobRole.Add(JobRole);
            }

            // Benefits (many)
            foreach (var benefit in Benefit ?? Enumerable.Empty<string>())
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
        [HttpPost]
        public async Task<IActionResult> TrainingCreate(TrainingCreateViewModel model,  List<string> Requirement, List<string> Benefit)
        {
            ModelState.Keys
            .Where(k => k.StartsWith("Uploads"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
            ModelState.Keys
            .Where(k => k.Equals("Badge.Training"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
            ModelState.Keys
            .Where(k => k.Equals("Badge.TrainingId"))
            .ToList()
            .ForEach(k => ModelState.Remove(k));
            if (model.PaymentOption == "Full")
            {
                ModelState.Remove("DownPayment");
                ModelState.Remove("Unit");
            }
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fill up all the details.";
                return View(model);
            }
            

            var train = new Training
            {
                UsersId = int.Parse(User.FindFirst("UsersId")?.Value),
                Name = model.Name,
                Price = model.Price,
                Expiration = model.Expiration,
                TrainingDescription = model.TrainingDescription,
                Duration = model.Duration,
                Diploma = false,
                Resume = false,
                Tor = false,
                Coe = false,
                SeamansBook = false,
                PassportId = false,
                PaymentOption = model.PaymentOption,
                ModeOfPayment = model.ModeOfPayment,
                DownPayment = model.DownPayment,
                Unit = model.Unit
            };

            _context.Training.Add(train);
            await _context.SaveChangesAsync();

            foreach (var data in model.Uploads)
            {
                var trainUploads = new TrainingUploads
                {
                    TrainingId = train.Id,
                    Name = data.Name,
                    isActive = data.isActive
                };

                _context.TrainingUploads.Add(trainUploads);
                await _context.SaveChangesAsync();
            }
            var badge = new TrainingBadge
            {
                TrainingId = train.Id,
                Badge = model.Badge.Badge,
                Description = model.Badge.Description,
                Validity = model.Badge.Validity
            };

            _context.TrainingBadge.Add(badge);
            await _context.SaveChangesAsync();
            // Requirements (many)
            foreach (var requirement in Requirement ?? Enumerable.Empty<string>())
            {
                var TrainingRequirements = new TrainingRequirements
                {
                    TrainingId = train.Id,
                    Requirement = requirement
                };
                _context.TrainingRequirements.Add(TrainingRequirements);
            }

            // Benefits (many)
            foreach (var benefit in Benefit ?? Enumerable.Empty<string>())
            {
                var TrainingBenefits = new TrainingBenefits
                {
                    TrainingId = train.Id,
                    Benefit = benefit
                };
                _context.TrainingBenefits.Add(TrainingBenefits);
            }

            // Media (many)
            foreach (var file in model.TrainingMedia ?? Enumerable.Empty<IFormFile>())
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
                    var TrainingMedia = new TrainingMedia
                    {
                        TrainingId = train.Id,
                        FilePath = $"/uploads/{fileName}",
                        FileType = file.ContentType
                    };
                    _context.TrainingMedia.Add(TrainingMedia);
                }
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Training added successfully!";
            return RedirectToAction("JobListing");
        }
        [HttpGet]
        public async Task<IActionResult> TrashedView(int id, int RecipientId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UsersId")?.Value);

                var draftCount = _context.EmailThreads
                    .Include(t => t.Emails)
                    .Where(t => t.Emails.Any(e => e.IsDraft && e.SenderID == currentUserId))
                    .Count();
                var inboxCount = _context.EmailRecipients
                    .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead)
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
                        t.Emails.Any(e =>
                            (e.SenderID == currentUserId &&
                             e.Recipients.Any(r => r.RecipientID == currentUserId && r.IsTrashed)) ||
                            e.Recipients.Any(r => r.RecipientID == currentUserId && r.IsTrashed)
                        )
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

                var changeisRead = _context.EmailRecipients.Where(u => u.Email.ThreadID == id && u.RecipientID == currentUserId).ToList();
                if (changeisRead.Count > 0)
                {
                    foreach (var data in changeisRead)
                    {
                        data.IsRead = true;
                    }
                    _context.SaveChanges();
                }

                var draftCount = _context.EmailThreads
                    .Include(t => t.Emails)
                    .Where(t => t.Emails.Any(e => e.IsDraft && e.SenderID == currentUserId))
                    .Count();
                var inboxCount = _context.EmailRecipients
                    .Where(r => r.RecipientID == currentUserId && !r.IsTrashed && !r.IsRead && !r.Email.IsTrashed && !r.Email.Thread.IsTrashed)
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
                        t.Id == id
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
                    .Where(t => t.Emails.Any(e => e.IsDraft && e.SenderID == currentUserId))
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
    }
}
