using burbodek.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace burbodek.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Ensure database is created and latest migrations applied
            context.Database.EnsureCreated();

            // Check if admin already exists
            if (!context.Users.Any(u => u.Email == "admin@test.com"))
            {
                var hasher = new PasswordHasher<Users>();

                var admin = new Users
                {
                    Username = "Administrator",
                    Email = "admin@test.com",
                    DateCreated = DateTime.Now,
                    Role = "Admin"
                };

                // Hash password at runtime
                admin.Password = hasher.HashPassword(admin, "12345678");

                context.Users.Add(admin);
                context.SaveChanges();
            }
            if (!context.Plans.Any(u => u.PlanName == "Basic"
                         || u.PlanName == "Monthly"
                         || u.PlanName == "Annual"))
            {
                var plans = new List<Plans>
                    {
                        new Plans
                        {
                            PlanName = "Basic",
                            PlanDetails = "For individuals starting out",
                            Price = 0,
                            Discount = 0
                        },
                        new Plans
                        {
                            PlanName = "Monthly",
                            PlanDetails = "Enjoy 31 days of our product for only 1999 only per month.",
                            Price = 1999,
                            Discount = 0
                        },
                        new Plans
                        {
                            PlanName = "Annual",
                            PlanDetails = "Enjoy a whole year of our product without any limitation with 20% off from the original price!",
                            Price = 23988,
                            Discount = 20
                        }
                    };

                context.Plans.AddRange(plans);
                context.SaveChanges();
            }
        }

        }
    }
