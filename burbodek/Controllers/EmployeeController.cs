using burbodek.Data;
using burbodek.Models;
using burbodek.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace burbodek.Controllers
{
    [Authorize(Roles = "Client")]
    public class EmployeeController : Controller
    {
        ApplicationDbContext _context;
        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index(string keyword, string location, int page = 1)
        {
            int pageSize = 10;
            var userId = int.Parse(User.FindFirst("UsersId")?.Value);

            var query = _context.Jobs
                .Include(j => j.Users)
                    .ThenInclude(u => u.EmployerDetails)
                .Include(j => j.JobApplication)
                .Where(j => j.ExpirationDate > DateTime.Now && j.isArchived == null);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(j =>
                    j.JobTitle.Contains(keyword) ||
                    j.JobDescription.Contains(keyword) ||
                    j.JobRole.Any(r => r.Role.Contains(keyword)));
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(j => j.Users.EmployerDetails.Address.Contains(location));
            }

            int totalJobs = query.Count();

            var jobs = query
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

            var viewModel = new JobListViewModel
            {
                Jobs = jobs,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalJobs / (double)pageSize),
                Keyword = keyword,
                Location = location
            };

            return View(viewModel);
        }


        public IActionResult Dashboard()
        {
            return View();
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
            return RedirectToAction("JobApply", new { id = Id });
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
