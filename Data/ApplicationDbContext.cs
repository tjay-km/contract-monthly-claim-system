using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add your DbSet properties here for your custom models
        // public DbSet<YourModel> YourModels { get; set; }


        public DbSet<Claim> Claims { get; set; }

    }
}