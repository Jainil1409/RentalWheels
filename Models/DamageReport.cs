using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace vehicle_management_system_mvc.Models
{
    public class DamageReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal DamageCost { get; set; }

        public bool IsPaid { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string PdfUrl { get; set; } = string.Empty;
    }
}
