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
        public DbSet<Subscription> Subscription { get; set; }

        public DbSet<PaymentDetails> PaymentDetails { get; set; }
        public override int SaveChanges()
        {
            UpdateExpiredSubscriptions();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateExpiredSubscriptions();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateExpiredSubscriptions()
        {
            var subscriptions = ChangeTracker.Entries<Subscription>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity);

            foreach (var subscription in subscriptions)
            {
                subscription.CheckAndUpdateExpiration();
            }
        }
    }
}