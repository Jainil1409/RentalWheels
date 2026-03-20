using System.ComponentModel.DataAnnotations;

namespace vehicle_management_system_mvc.Models
{
    public enum VehicleType
    {
        Sedan,
        SUV,
        Truck,
        Van,
        Hatchback,
        Luxury,
        Convertible
    }

    public class Vehicle
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Brand { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required]
        public VehicleType Type { get; set; }

        [Required, Range(0.01, 100000)]
        [Display(Name = "Price Per Day")]
        public decimal PricePerDay { get; set; }

        [Required, Range(0, 50000)]
        [Display(Name = "Security Deposit")]
        public decimal DepositAmount { get; set; }

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(300)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Range(1900, 2100)]
        public int Year { get; set; }

        [StringLength(20)]
        [Display(Name = "License Plate")]
        public string? LicensePlate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Booking> Bookings { get; set; } = [];
    }
}
