using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SystemClaim.Models;

namespace SystemClaim.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Userss { get; set; }
        public DbSet<Claims> Claims { get; set; }

        public DbSet<UploadDocument> UploadDocuments { get; set; }
    }
}
