using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add your DbSet properties here for your custom models
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimDocument> ClaimDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Claim>(entity =>
            {
                entity.Property(e => e.LecturerName).IsRequired();
                entity.Property(e => e.HoursWorked).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Status).HasDefaultValue("Pending").IsRequired();
                entity.Property(e => e.AdditionalNotes).IsRequired(false);
                entity.Property(e => e.RejectionReason).IsRequired(false);
                entity.Property(e => e.SupportingDocumentPath).IsRequired(false);
                entity.Property(e => e.UserId).IsRequired(false);
                entity.Ignore(e => e.SupportingDocument); // Ignore IFormFile as it's not stored in DB
                
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ClaimDocument>(entity =>
            {
                entity.Property(e => e.FileName).IsRequired();
                entity.Property(e => e.FilePath).IsRequired();
                entity.HasOne<Claim>()
                    .WithMany()
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}