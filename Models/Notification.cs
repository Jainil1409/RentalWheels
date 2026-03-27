using System.ComponentModel.DataAnnotations;

namespace vehicle_management_system_mvc.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Type { get; set; } = "General"; // Payment, DamagePenalty, etc.

        // Optional: Link to a specific booking
        public int? BookingId { get; set; }

        // Optional: Link to a specific user (for user-facing notifications like deposit refund)
        public int? UserId { get; set; }
    }
}