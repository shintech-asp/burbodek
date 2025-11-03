namespace burbodek.Models
{
    public class TrainingPayments
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public int TrainingApplicationId { get; set; }
        public TrainingApplication TrainingApplication { get; set; }
        public string PaymentOption { get; set; }
        public decimal Price { get; set; }
        public decimal? Paid { get; set; }
        public string ModeOfPayment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
