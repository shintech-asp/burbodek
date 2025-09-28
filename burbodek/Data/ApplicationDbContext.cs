using burbodek.Migrations;
using burbodek.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace burbodek.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Plans> Plans { get; set; }
        public DbSet<EmployerDetails> EmployerDetails { get; set; }
        public DbSet<Files> Files { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<Models.Subscription> Subscription { get; set; }

        public DbSet<Models.PaymentDetails> PaymentDetails { get; set; }
        public void UpdateExpiredSubscriptionsOnStartup()
        {
            var basicPlan = Plans.FirstOrDefault(p => p.PlanName == "Basic");
            if (basicPlan == null) return;

            // Grab all expired subs (Current or Renewed) grouped by user
            var expiredByUser = Subscription
                .Where(s => (s.Status == "Current" || s.Status == "Renewed")
                            && s.Expiration.HasValue
                            && s.Expiration.Value < DateTime.Now)
                .GroupBy(s => s.UsersId)
                .ToList();

            // Find users who already have a Current Basic (don’t add another)
            var existingCurrentBasics = Subscription
                .Where(s => s.PlansId == basicPlan.Id && s.Status == "Current")
                .Select(s => s.UsersId)
                .ToHashSet();

            foreach (var group in expiredByUser)
            {
                int userId = group.Key;

                // Mark all expired subs for this user as Expired
                foreach (var sub in group)
                {
                    sub.Status = "Expired";
                }

                // Add back Basic only if they don’t already have a Current one
                if (!existingCurrentBasics.Contains(userId))
                {
                    Subscription.Add(new Models.Subscription
                    {
                        UsersId = userId,
                        PlansId = basicPlan.Id,
                        Status = "Current",
                        Expiration = null,
                        CreatedAt = DateTime.Now
                    });

                    existingCurrentBasics.Add(userId); // prevent duplicate adds in same run
                }
            }
        }


    }
}