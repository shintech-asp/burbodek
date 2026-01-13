using burbodek.Migrations;

namespace burbodek.Models.ViewModels
{
    public class TrainingCreateViewModel
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime Expiration { get; set; }
        public string Duration { get; set; }
        public string TrainingDescription { get; set; }

        public List<string> TrainingRequirements { get; set; } = new();
        public List<string> TrainingBenefits { get; set; } = new();

        public List<IFormFile> TrainingMedia { get; set; } = new();
        public List<TrainingUploads> Uploads { get; set; } = new();
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
    }
}
