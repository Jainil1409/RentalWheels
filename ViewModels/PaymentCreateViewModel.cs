using System.ComponentModel.DataAnnotations;
using vehicle_management_system_mvc.Models;

namespace vehicle_management_system_mvc.ViewModels
{
    public class PaymentCreateViewModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        public Booking? Booking { get; set; }

        public string StripePublishableKey { get; set; } = "";
    }
}
