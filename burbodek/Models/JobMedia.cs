using System.ComponentModel.DataAnnotations;

namespace burbodek.Models
{
    public class JobMedia
    {
        [Key] public int Id { get; set; }
        public string FilePath { get; set; } 
        public string FileType { get; set; } 
        public int JobsId { get; set; } 
        public Jobs Jobs { get; set; }
    }
}
