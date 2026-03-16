using System.ComponentModel.DataAnnotations;

namespace vehicle_management_system_mvc.Models
{
    public enum UserRole
    {
        Customer,
        Admin
    }

    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Please enter a valid 10-digit phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Customer;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // KYC & Profile Information
        [StringLength(50)]
        public string? DriverLicenseNumber { get; set; }
        
        public DateTime? LicenseExpiryDate { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        public string? IdProofUrl { get; set; }
        
        public bool IsVerified { get; set; } = false;

        public ICollection<Booking> Bookings { get; set; } = [];
    }
}
