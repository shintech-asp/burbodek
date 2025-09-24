namespace burbodek.Models
{
    public class Payments
    {
        public int Id { get; set; }
        public double? Amount { get; set; }
        public string PaymentDetails { get; set; }
        public string Status { get; set; } = "Paid";
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
