using System.ComponentModel.DataAnnotations;

namespace vehicle_management_system_mvc.Models
{
    public enum BookingStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed,
        Cancelled
    }

    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public User Customer { get; set; } = null!;

        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        [Required, DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Total Cost")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Security Deposit")]
        public decimal DepositAmount { get; set; }

        [Display(Name = "Deposit Deducted")]
        public decimal DepositDeducted { get; set; }

        [Display(Name = "Deposit Refunded")]
        public decimal DepositRefunded { get; set; }

        [Display(Name = "Is Deposit Refunded")]
        public bool IsDepositRefunded { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Payment? Payment { get; set; }
        public DamageReport? DamageReport { get; set; }
    }
}
