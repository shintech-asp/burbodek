using System.ComponentModel.DataAnnotations.Schema;

namespace burbodek.Models
{
    public class ApplicantJobUpload
    {
        public int Id { get; set; }
        public int JobUploadsId { get; set; }
        public JobUploads JobUploads { get; set; }
        public int? UsersId { get; set; }
        public Users? Users { get; set; }
        public string? Upload { get; set; }

        [NotMapped]
        public IFormFile? File { get; set; }
    }
}
