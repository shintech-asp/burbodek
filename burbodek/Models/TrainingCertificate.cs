using System.ComponentModel.DataAnnotations;

namespace burbodek.Models
{
    public class TrainingCertificate
    {
        [Key] public int Id { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public int TrainingApplicationId { get; set; }
        public TrainingApplication TrainingApplication { get; set; }
    }
}
