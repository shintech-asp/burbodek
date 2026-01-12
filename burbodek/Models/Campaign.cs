namespace burbodek.Models
{
    public class Campaign
    {
        public int Id { get; set; }

        // Step 1: Campaign Details
        public string CampaignName { get; set; }
        public string CampaignDescription { get; set; }
        public string LogoFilePath { get; set; }

        // Step 2: Listing Selection
        public int? SelectedListingId { get; set; }
        public string ListingType { get; set; } // "Job" or "Training"

        // Step 3: Location
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string FullAddress { get; set; }

        // Metadata
        public int CreatedByUserId { get; set; }
        public Users CreatedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public Jobs? SelectedJob { get; set; }
        public Training? SelectedTraining { get; set; }
        public int TotalClicks { get; set; }

        public int? PaymentDetailsId { get; set; }
        public PaymentDetails? PaymentDetails { get; set; }
        public decimal? Payment { get; set; }
        public bool? isPaid { get; set; }
    }
}
