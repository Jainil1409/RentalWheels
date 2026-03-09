using System.ComponentModel.DataAnnotations;

namespace vehicle_management_system_mvc.Models
{
    public enum PaymentMethod
    {
        Cash,
        [Display(Name = "Stripe")]
        Stripe
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        public PaymentMethod Method { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [StringLength(200)]
        public string? StripePaymentIntentId { get; set; }

        [StringLength(50)]
        public string? InvoiceNumber { get; set; }
    }
}
