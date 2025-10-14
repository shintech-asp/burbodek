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
        public DateTime DurationFrom { get; set; }
        public DateTime DurationTo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? isArchived { get; set; }
        public ICollection<TrainingRequirements> TrainingRequirements { get; set; } = new List<TrainingRequirements>();
        public ICollection<TrainingBenefits> TrainingBenefits { get; set; } = new List<TrainingBenefits>();
        public ICollection<TrainingMedia> TrainingMedia { get; set; } = new List<TrainingMedia>();
    }
}
