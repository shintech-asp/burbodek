namespace burbodek.Models.ViewModels
{
    public class TrainingCreateViewModel
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime DurationFrom { get; set; }
        public DateTime DurationTo { get; set; }
        public DateTime Expiration { get; set; }
        public string TrainingDescription { get; set; }

        public List<string> TrainingRequirements { get; set; } = new();
        public List<string> TrainingBenefits { get; set; } = new();

        public List<IFormFile> TrainingMedia { get; set; } = new();
    }
}
