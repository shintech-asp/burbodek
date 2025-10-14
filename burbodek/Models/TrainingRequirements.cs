namespace burbodek.Models
{
    public class TrainingRequirements
    {
        public int Id { get; set; }
        public string Requirement { get; set; }
        public int TrainingId { get; set; }
        public Training Training { get; set; }
    }
}
