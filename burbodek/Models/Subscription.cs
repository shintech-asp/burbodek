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
        public string? Status { get; set; } //Current, Expired, Once availed a new one or renewed the status will be changed to Renewed, No Expiration = Pending

        public void CheckAndUpdateExpiration()
        {
            if (Expiration.HasValue && Expiration.Value <= DateTime.Now)
            {
                Status = "Expired";
            }
        }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
