using System.ComponentModel.DataAnnotations;

namespace burbodek.Models
{
    public class JobRequirements
    {
        [Key] public int Id { get; set; }
        public string Requirement { get; set; }

        
        public int JobsId { get; set; } 
        public Jobs Jobs { get; set; }
    }
}
