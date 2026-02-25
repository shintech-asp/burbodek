namespace burbodek.Models
{
    public class Training
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime Expiration { get; set; }
        public string TrainingDescription { get; set; }
        public string Duration { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? isArchived { get; set; }
        public ICollection<TrainingRequirements> TrainingRequirements { get; set; } = new List<TrainingRequirements>();
        public ICollection<TrainingBenefits> TrainingBenefits { get; set; } = new List<TrainingBenefits>();
        public ICollection<TrainingMedia> TrainingMedia { get; set; } = new List<TrainingMedia>();
        public ICollection<TrainingApplication> TrainingApplication { get; set; } = new List<TrainingApplication>();
        public ICollection<TrainingUploads> TrainingUploads { get; set; } = new List<TrainingUploads>();
        public TrainingBadge TrainingBadge { get; set; }
        public List<PostReport>? PostReport { get; set; }
        public bool Diploma { get; set; }
        public bool Resume { get; set; }
        public bool PassportId { get; set; }
        public bool SeamansBook { get; set; }
        public bool Tor { get; set; }
        public bool Coe { get; set; }
        public string PaymentOption { get; set; }
        public string ModeOfPayment { get; set; }
        public decimal? DownPayment { get; set; }
        public string? Unit { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
