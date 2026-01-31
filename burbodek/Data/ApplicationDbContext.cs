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
        public DbSet<Jobs> Jobs { get; set; }
        public DbSet<JobBenefits> JobBenefits { get; set; }
        public DbSet<EmployeeDetails> EmployeeDetails { get; set; }
        public DbSet<JobRole> JobRole { get; set; }
        public DbSet<Training> Training { get; set; }
        public DbSet<TrainingBenefits> TrainingBenefits { get; set; }
        public DbSet<TrainingRequirements> TrainingRequirements { get; set; }
        public DbSet<UserBadge> UserBadge { get; set; }
        public DbSet<JobRequiredBadge> JobRequiredBadge { get; set; }
        public DbSet<TrainingBadge> TrainingBadge { get; set; }
        public DbSet<TrainingMedia> TrainingMedia { get; set; }
        public DbSet<JobMedia> JobMedia { get; set; }
        public DbSet<JobApplication> JobApplication { get; set; }
        public DbSet<JobRequirements> JobRequirements { get; set; }
        public DbSet<TrainingPayments> TrainingPayments { get; set; }
        public DbSet<TrainingApplication> TrainingApplication { get; set; }
        public DbSet<TrainingCertificate> TrainingCertificate { get; set; }
        public DbSet<ApplicantTrainingUpload> ApplicantTrainingUpload { get; set; }
        public DbSet<ApplicantJobUpload> ApplicantJobUpload { get; set; }
        public DbSet<Subscription> Subscription { get; set; }
        public DbSet<EmailThread> EmailThreads { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<Campaign> Campaign { get; set; }
        public DbSet<Faq> Faq { get; set; }
        public DbSet<FaqTitle> FaqTitle { get; set; }
        public DbSet<JobUploads> JobUploads { get; set; }
        public DbSet<TrainingUploads> TrainingUploads { get; set; }
        public DbSet<EmailRecipient> EmailRecipients { get; set; }
        public DbSet<EmailAttachment> EmailAttachments { get; set; }
        public DbSet<EmailTemplate> EmailTemplate { get; set; }
        public DbSet<PaymentDetails> PaymentDetails { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmailRecipient>()
                .HasOne(er => er.Email)
                .WithMany(e => e.Recipients)
                .HasForeignKey(er => er.EmailID);

            modelBuilder.Entity<Email>()
                .HasOne(e => e.Thread)
                .WithMany(t => t.Emails)
                .HasForeignKey(e => e.ThreadID);
        }
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
                    Subscription.Add(new Subscription
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