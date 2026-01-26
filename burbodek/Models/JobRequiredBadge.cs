namespace burbodek.Models
{
    public class JobRequiredBadge
    {
        public int Id { get; set; }
        public int JobsId { get; set; }
        public Jobs Jobs { get; set; }
        public string Badge { get; set; }
    }
}
