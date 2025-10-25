using burbodek.Data;
using burbodek.Models;
using burbodek.Models.ViewModels;
using burbodek.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Client")]
    public class EmployeeController : Controller
    {
        ApplicationDbContext _context;
        private readonly IPaymongo _paymongo;
        public EmployeeController(ApplicationDbContext context,IPaymongo paymongo)
        {
            _context = context;
            _paymongo = paymongo;
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
