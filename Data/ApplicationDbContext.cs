using Microsoft.EntityFrameworkCore;
using vehicle_management_system_mvc.Models;

namespace vehicle_management_system_mvc.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<DamageReport> DamageReports => Set<DamageReport>();
        public DbSet<Notification> Notifications => Set<Notification>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.Property(v => v.PricePerDay).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(b => b.TotalCost).HasColumnType("decimal(18,2)");

                entity.HasOne(b => b.Customer)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Vehicle)
                      .WithMany(v => v.Bookings)
                      .HasForeignKey(b => b.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.Payment)
                      .HasForeignKey<Payment>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DamageReport>(entity =>
            {
                entity.Property(d => d.DamageCost).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Booking)
                      .WithOne(b => b.DamageReport)
                      .HasForeignKey<DamageReport>(d => d.BookingId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
