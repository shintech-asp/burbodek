namespace burbodek.Models
{
    public class PaymentDetails
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public Users Users { get; set; }
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
    }
}
