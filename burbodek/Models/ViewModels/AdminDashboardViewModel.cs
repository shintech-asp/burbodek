namespace burbodek.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int NewUsersThisWeek { get; set; }

        public List<Users> NewUsers { get; set; } = new();
        public List<Subscription> LatestSubscriptions { get; set; } = new();
        public List<DailyRevenueItem> DailyRevenue { get; set; } = new();

        // Campaign stats
        public int TotalCampaigns { get; set; }
        public int ActiveCampaigns { get; set; }
        public decimal CampaignRevenue { get; set; }
        public List<Campaign> LatestCampaigns { get; set; } = new();
    }

    public class DailyRevenueItem
    {
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
    }
}
