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
        public List<JobApplication>? JobApplication { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
