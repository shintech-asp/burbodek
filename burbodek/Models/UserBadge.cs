namespace burbodek.Models
{
    public class UserBadge
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public string Badge { get; set; }
        public DateTime ValidUntil { get; set; }

    }
}
