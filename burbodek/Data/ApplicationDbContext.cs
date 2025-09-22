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
    }
}
