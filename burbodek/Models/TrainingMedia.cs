using System.ComponentModel.DataAnnotations;

namespace burbodek.Models
{
    public class TrainingMedia
    {
        [Key] public int Id { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public int TrainingId { get; set; }
        public Training Training { get; set; }
    }
}
