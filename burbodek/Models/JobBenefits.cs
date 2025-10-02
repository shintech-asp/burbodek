using System.ComponentModel.DataAnnotations;

namespace burbodek.Models
{
    public class JobBenefits
    {
        [Key] public int Id { get; set; }
        public string Benefit { get; set; } 
        public int JobsId { get; set; } 
        public Jobs Jobs { get; set; }
    }
}
