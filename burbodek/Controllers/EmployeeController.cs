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


            return View(data);
        }
        [HttpPost]
        public async Task<IActionResult> JobApply(JobApplication model, int Id, IFormFile CVUpload)
        { 
            ModelState.Remove("Jobs");
            ModelState.Remove("AppliedBy");
            ModelState.Remove("CV");

            model.JobsId = Id;
            model.AppliedBy = int.Parse(User.FindFirst("UsersId")?.Value);

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fill all the required fields!";
                return View(model);
            }

            string cvPath = null;

            if (CVUpload != null && CVUpload.Length > 0)
            {
                // Ensure uploads folder exists
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cv");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(CVUpload.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file in wwwroot/uploads/cv/
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await CVUpload.CopyToAsync(stream);
                }

                // Save relative path only (to serve via browser later)
                cvPath = "/uploads/cv/" + uniqueFileName;
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
                CV = cvPath // ✅ only path is stored, not the actual file
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
