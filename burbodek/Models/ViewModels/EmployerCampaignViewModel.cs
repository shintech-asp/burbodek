namespace burbodek.Models.ViewModels
{
    public class EmployerCampaignViewModel
    {
        public Subscription Subscription { get; set; }
        public List<Jobs> Jobs { get; set; }
        public List<Training> Training { get; set; }
        public List<Campaign> Campaign { get; set; }
        public List<PaymentDetails> PaymentDetails { get; set; }
    }
}
