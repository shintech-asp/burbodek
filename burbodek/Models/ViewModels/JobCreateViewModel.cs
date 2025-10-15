namespace burbodek.Models.ViewModels
{
    public class JobCreateViewModel
    {
        public string JobTitle { get; set; }
        public string JobType { get; set; }
        public int SalaryMin { get; set; }
        public int SalaryMax { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string JobDescription { get; set; }

        public List<string> JobRequirements { get; set; } = new();
        public List<string> JobBenefits { get; set; } = new();
        public List<string> JobRole { get; set; } = new();

        public List<IFormFile> JobMedia { get; set; } = new();
        public bool Diploma { get; set; }
        public bool Resume { get; set; }
        public bool PassportId { get; set; }
        public bool SeamansBook { get; set; }
        public bool Tor { get; set; }
        public bool Coe { get; set; }
    }

}
