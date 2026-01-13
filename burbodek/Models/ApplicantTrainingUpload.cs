using System.ComponentModel.DataAnnotations.Schema;

namespace burbodek.Models
{
    public class ApplicantTrainingUpload
    {
        public int Id { get; set; }
        public int TrainingUploadsId { get; set; }
        public TrainingUploads TrainingUploads { get; set; }
        public string? Upload { get; set; }

        [NotMapped]
        public IFormFile? File { get; set; }
    }
}
