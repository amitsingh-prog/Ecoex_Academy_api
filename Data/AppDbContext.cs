using Ecoeex_Academy_Api.Model;
using Ecoex_Academy_Api.Model;
using Microsoft.EntityFrameworkCore;

namespace Ecoeex_Academy_Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> tb_Courses { get; set; }
        public DbSet<PricingSetting> tb_PricingSettings { get; set; }
        public DbSet<User> tb_Users { get; set; }
        public DbSet<RegistrationGroup> tb_RegistrationGroups { get; set; }
        public DbSet<RegistrationGroupMember> tb_RegistrationGroupMembers { get; set; }
        public DbSet<OtpRequest> tb_OtpRequests { get; set; }
        public DbSet<Order> tb_Orders { get; set; }
        public DbSet<OrderCourse> tb_OrderCourses { get; set; }
        public DbSet<Payment> tb_Payments { get; set; }
        public DbSet<Enrollment> tb_Enrollments { get; set; }
        public DbSet<ZoomAccess> tb_ZoomAccess { get; set; }
        public DbSet<Certificate> tb_Certificates { get; set; }
        public DbSet<RecordingAccess> tb_RecordingAccess { get; set; }
        public DbSet<AdminUser> tb_AdminUsers { get; set; }
        public DbSet<tb_social_media_count> tb_social_media_count { get; set; }

        // Existing table
        public DbSet<tb_userdetail> tb_userdetail { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------------
            // Table Mapping
            // -----------------------------
            modelBuilder.Entity<Course>().ToTable("tb_Courses");
            modelBuilder.Entity<User>().ToTable("tb_Users");
            modelBuilder.Entity<PricingSetting>().ToTable("tb_PricingSettings");
            modelBuilder.Entity<RegistrationGroup>().ToTable("tb_RegistrationGroups");
            modelBuilder.Entity<RegistrationGroupMember>().ToTable("tb_RegistrationGroupMembers");
            modelBuilder.Entity<OtpRequest>().ToTable("tb_OtpRequests");
            modelBuilder.Entity<Order>().ToTable("tb_Orders");
            modelBuilder.Entity<OrderCourse>().ToTable("tb_OrderCourses");
            modelBuilder.Entity<Payment>().ToTable("tb_Payments");
            modelBuilder.Entity<Enrollment>().ToTable("tb_Enrollments");
            modelBuilder.Entity<ZoomAccess>().ToTable("tb_ZoomAccess");
            modelBuilder.Entity<Certificate>().ToTable("tb_Certificates");
            modelBuilder.Entity<RecordingAccess>().ToTable("tb_RecordingAccess");
            modelBuilder.Entity<AdminUser>().ToTable("tb_AdminUsers");
            modelBuilder.Entity<tb_social_media_count>().ToTable("tb_social_media_count");

            // -----------------------------
            // Composite Keys
            // -----------------------------
            modelBuilder.Entity<OrderCourse>()
                .HasKey(x => new { x.OrderId, x.CourseID });



            // -----------------------------
            // Relationships
            // -----------------------------


            modelBuilder.Entity<RegistrationGroupMember>()
             .HasOne(x => x.PrimaryUser)
             .WithMany()
             .HasForeignKey(x => x.PrimaryUserId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegistrationGroupMember>()
                .HasOne(x => x.MemberUser)
                .WithMany()
                .HasForeignKey(x => x.MemberUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegistrationGroupMember>()
                .HasOne(x => x.Group)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.GroupId);




            modelBuilder.Entity<Order>()
                .HasOne(x => x.PayerUser)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.PayerUserId);

            modelBuilder.Entity<OrderCourse>()
                .HasOne(x => x.Order)
                .WithMany(x => x.OrderCourses)
                .HasForeignKey(x => x.OrderId);

            modelBuilder.Entity<OrderCourse>()
                .HasOne(x => x.Course)
                .WithMany(x => x.OrderCourses)
                .HasForeignKey(x => x.CourseID);

            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Order)
                .WithOne(x => x.Payment)
                .HasForeignKey<Payment>(x => x.OrderId);

            modelBuilder.Entity<Enrollment>()
                .HasOne(x => x.Order)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.OrderId);

            modelBuilder.Entity<Enrollment>()
                .HasOne(x => x.User)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<Enrollment>()
                .HasOne(x => x.Course)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.CourseID);

            modelBuilder.Entity<ZoomAccess>()
                .HasOne(x => x.Enrollment)
                .WithMany(x => x.ZoomAccesses)
                .HasForeignKey(x => x.EnrollmentId);

            modelBuilder.Entity<ZoomAccess>()
                .HasOne(x => x.User)
                .WithMany(x => x.ZoomAccesses)
                .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<Certificate>()
                .HasOne(x => x.Enrollment)
                .WithMany(x => x.Certificates)
                .HasForeignKey(x => x.EnrollmentId);

            modelBuilder.Entity<Certificate>()
                .HasOne(x => x.User)
                .WithMany(x => x.Certificates)
                .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<Certificate>()
                .HasOne(x => x.Course)
                .WithMany(x => x.Certificates)
                .HasForeignKey(x => x.CourseID);

            modelBuilder.Entity<RecordingAccess>()
                .HasOne(x => x.Enrollment)
                .WithMany(x => x.RecordingAccesses)
                .HasForeignKey(x => x.EnrollmentId);

            modelBuilder.Entity<RecordingAccess>()
                .HasOne(x => x.User)
                .WithMany(x => x.RecordingAccesses)
                .HasForeignKey(x => x.UserId);

            modelBuilder.Entity<RecordingAccess>()
                .HasOne(x => x.Course)
                .WithMany(x => x.RecordingAccesses)
                .HasForeignKey(x => x.CourseID);
        }
    }
}