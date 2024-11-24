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
        public DbSet<ClaimAudit> ClaimAudits { get; set; }
        public DbSet<ClaimReport> ClaimReports { get; set; }
        public DbSet<AutoApprovalConfig> AutoApprovalConfigs { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Module> Modules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("AspNetUsers");
                
                // Required fields with default values
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(256);
                entity.Property(e => e.NormalizedUserName).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.NormalizedEmail).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

                // Optional fields
                entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.LastName).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.Department).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.Specialization).HasMaxLength(100).IsRequired(false);

                // Enums and booleans
                entity.Property(e => e.UserType).HasConversion<string>().IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true).IsRequired();

                // Identity specific fields
                entity.Property(e => e.SecurityStamp).IsRequired();
                entity.Property(e => e.ConcurrencyStamp).IsRequired();
            });

            modelBuilder.Entity<Claim>(entity =>
            {
                entity.ToTable("Claims");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LecturerId).IsRequired().HasMaxLength(256);
                entity.Property(e => e.LecturerName).IsRequired().HasMaxLength(256);
                entity.Property(e => e.LecturerDepartment).IsRequired().HasMaxLength(256);
                entity.Property(e => e.HoursWorked).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.HourlyRate).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.SubmissionDate).IsRequired();
                entity.Property(e => e.AdditionalNotes).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
                entity.Property(e => e.ModuleCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RejectionReason).HasMaxLength(500);
                entity.Property(e => e.SupportingDocumentPath).HasMaxLength(1000);
                entity.Property(e => e.ReviewedBy).HasMaxLength(256);
                entity.Property(e => e.ApprovalComments).HasMaxLength(500);

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(c => c.Reviewer)
                    .WithMany()
                    .HasForeignKey(c => c.ReviewerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(c => c.Documents)
                    .WithOne(d => d.Claim)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Department)
                    .WithMany()
                    .HasForeignKey(d => d.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Module)
                    .WithMany()
                    .HasForeignKey(d => d.ModuleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ClaimDocument>(entity =>
            {
                entity.ToTable("ClaimDocuments");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.DateUploaded).IsRequired();

                entity.HasOne(d => d.Claim)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired();
            });

            modelBuilder.Entity<ClaimAudit>(entity =>
            {
                entity.ToTable("ClaimAudits");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Details).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Timestamp).IsRequired();

                entity.HasOne(d => d.Claim)
                    .WithMany()
                    .HasForeignKey(d => d.ClaimId);
            });

            modelBuilder.Entity<ClaimReport>(entity =>
            {
                entity.ToTable("ClaimReports");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.GeneratedBy).IsRequired().HasMaxLength(256);
                entity.Property(e => e.ReportType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.GeneratedDate).IsRequired();
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
            });

            modelBuilder.Entity<AutoApprovalConfig>(entity =>
            {
                entity.ToTable("AutoApprovalConfigs");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.MaxAutoApprovalAmount).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.LastModifiedBy).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.LastModifiedDate).IsRequired();
                entity.Property(e => e.EnableAutoApproval).IsRequired();
            });

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Code)
                .IsUnique();

            modelBuilder.Entity<Module>()
                .HasIndex(m => m.Code)
                .IsUnique();

            modelBuilder.Entity<Module>()
                .HasOne(m => m.Department)
                .WithMany()
                .HasForeignKey(m => m.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Information Technology", Code = "IT", IsActive = true },
                new Department { Id = 2, Name = "Computer Science", Code = "CS", IsActive = true },
                new Department { Id = 3, Name = "Software Engineering", Code = "SE", IsActive = true }
            );

            modelBuilder.Entity<Module>().HasData(
                new Module { Id = 1, Code = "IT101", Name = "Introduction to Programming", DepartmentId = 1, IsActive = true },
                new Module { Id = 2, Code = "IT102", Name = "Database Systems", DepartmentId = 1, IsActive = true },
                new Module { Id = 3, Code = "CS201", Name = "Data Structures", DepartmentId = 2, IsActive = true },
                new Module { Id = 4, Code = "CS202", Name = "Algorithms", DepartmentId = 2, IsActive = true },
                new Module { Id = 5, Code = "SE301", Name = "Software Design", DepartmentId = 3, IsActive = true },
                new Module { Id = 6, Code = "SE302", Name = "Software Testing", DepartmentId = 3, IsActive = true }
            );
        }
    }
}