namespace burbodek.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public int PlansId { get; set; }
        public Plans Plans { get; set; }
        public DateTime? Expiration { get; set; }
    }
}
