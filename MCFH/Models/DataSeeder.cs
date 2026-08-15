using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Models
{
    public static class DataSeeder
    {
        public static void SeedData(McfhDbContext context)
        {
            // Seed Roles
            if (!context.WorkspaceRoles.Any())
            {
                context.WorkspaceRoles.AddRange(
                    new WorkspaceRole { RoleName = "Owner" },
                    new WorkspaceRole { RoleName = "Editor" },
                    new WorkspaceRole { RoleName = "Viewer" }
                );
                context.SaveChanges();
            }

            // Seed Plans
            if (!context.SubscriptionPlans.Any())
            {
                context.SubscriptionPlans.AddRange(
                    new SubscriptionPlan { Name = "Basic", Price = 199000, AiCreditLimit = 500 },
                    new SubscriptionPlan { Name = "Premium", Price = 499000, AiCreditLimit = 2000 },
                    new SubscriptionPlan { Name = "Enterprise", Price = 999000, AiCreditLimit = 10000 }
                );
                context.SaveChanges();
            }

            // Seed Admin User
            if (!context.Users.Any(u => u.Email == "admin@gmail.com"))
            {
                var adminUser = new User
                {
                    Email = "admin@gmail.com",
                    PasswordHash = "$2a$11$EhXIn/jDaJnWw.OMzzsWLu.nzZ2E/I8ZpPI/UewoSYZk0pR7AXoKa", // 123
                    FullName = "System Admin",
                    AuthProvider = "local",
                    SystemRole = "Admin",
                    IsVerified = true,
                    VerifiedAt = DateTime.Now,
                    IsBanned = false,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(adminUser);
                context.SaveChanges();
            }
        }
    }
}
