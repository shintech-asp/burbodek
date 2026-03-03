using System.ComponentModel.DataAnnotations;

namespace burbodek.Models
{
    public class Jobs
    {
        [Key] public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        [Required, MaxLength(200)] public string JobTitle { get; set; }
        [Required, MaxLength(200)] public string JobType { get; set; }
        [Required][Range(0, int.MaxValue)] public int SalaryMin { get; set; }
        [Required][Range(0, int.MaxValue)] public int SalaryMax { get; set; }
        [Required][DataType(DataType.Date)] public DateTime ExpirationDate { get; set; }
        [Required] public string JobDescription { get; set; }
        public ICollection<JobRequirements> JobRequirements { get; set; } = new List<JobRequirements>();
        public ICollection<JobBenefits> JobBenefits { get; set; } = new List<JobBenefits>();
        public ICollection<JobMedia> JobMedia { get; set; } = new List<JobMedia>();
        public ICollection<JobRole> JobRole { get; set; } = new List<JobRole>();
        public ICollection<JobUploads> JobUploads { get; set; } = new List<JobUploads>();
        public ICollection<JobRequiredBadge> JobRequiredBadge { get; set; } = new List<JobRequiredBadge>();
        public List<JobApplication>? JobApplication { get; set; }
        public List<PostReport>? PostReport { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? isArchived { get; set; }
        public bool Diploma { get; set; }
        public bool Resume { get; set; }
        public bool PassportId { get; set; }
        public bool SeamansBook { get; set; }
        public bool Tor { get; set; }
        public bool Coe { get; set; }
        public bool? isDeleted { get; set; }
        public string? Appeal { get; set; }
        public bool? isFinal { get; set; }
    }
}
