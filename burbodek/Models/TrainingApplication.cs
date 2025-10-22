namespace burbodek.Models
{
    public class TrainingApplication
    {
        public int Id { get; set; }
        public int TrainingId { get; set; }
        public Training Training { get; set; }
        public int AppliedBy { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MobileNo { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public string? Diploma { get; set; }
        public string? Resume { get; set; }
        public string? PassportId { get; set; }
        public string? SeamansBook { get; set; }
        public string? Tor { get; set; }
        public string? Coe { get; set; }
        public ICollection<TrainingPayments> TrainingPayments { get; set; } = new List<TrainingPayments>();
        public string PaymentStatus { get; set; } = "Unpaid";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
