using System.ComponentModel.DataAnnotations;

namespace burbodek.Models
{
    public class TrainingBenefits
    {
        [Key] public int Id { get; set; }
        public string Benefit { get; set; }
        public int TrainingId { get; set; }
        public Training Training { get; set; }
    }
}
