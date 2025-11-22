using System;
using System.Threading.Tasks;
using SystemClaim.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SystemClaim.Data;

namespace SystemClaim
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            // Get required services from DI container
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Apply migrations
            context.Database.Migrate();

            // Create roles
            string[] roles = { "HR", "Lecturer", "Coordinator", "Manager" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Helper method to create Identity user + custom User
            async Task CreateUser(string email, string password, string role, string name, string surname, string department, decimal ratePerJob)
            {
                var identityUser = await userManager.FindByEmailAsync(email);
                if (identityUser == null)
                {
                    identityUser = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(identityUser, password);
                    if (!result.Succeeded)
                        throw new Exception($"Failed to create Identity user {email}: {string.Join(", ", result.Errors)}");

                    await userManager.AddToRoleAsync(identityUser, role);

                    // Add to custom Userss table
                    context.Userss.Add(new User
                    {
                        UserId = identityUser.Id,
                        Name = name,
                        Surname = surname,
                        Department = department,
                        DefaultRatePerJob = ratePerJob,
                        RoleName = role,
                        Email = email,       // IMPORTANT: NOT NULL column
                        password = password  // Optional if your DB requires it; else remove
                    });

                    await context.SaveChangesAsync();
                }
            }

            // Seed users
            await CreateUser("hr@site.com", "Hr@123!", "HR", "HR", "Manager", "Admin", 0);
            await CreateUser("pm@site.com", "Pm@1234!", "Coordinator", "Project", "Manager", "Projects", 0);
            await CreateUser("cm@site.com", "Cm@123!", "Manager", "Construction", "Manager", "Construction", 0);
            await CreateUser("worker@site.com", "Worker@123!", "Lecturer", "John", "Builder", "Masonry", 200);
        }
    }
}
