namespace burbodek.Models
{
    public class TrainingBadge
    {
        public int Id { get; set; }
        public int TrainingId { get; set; }
        public Training Training { get; set; }
        public string Badge { get; set; }
        public string? Description { get; set; }
        public DateTime Validity { get; set; }
    }
}
