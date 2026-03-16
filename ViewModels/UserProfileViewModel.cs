using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace vehicle_management_system_mvc.ViewModels
{
    public class UserProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Please enter a valid 10-digit phone number.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Driver's License Number is required to book vehicles.")]
        [Display(Name = "Driver's License Number")]
        public string DriverLicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License Expiry Date is required.")]
        [Display(Name = "License Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; }

        [Required(ErrorMessage = "Residential Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Upload ID/License Proof")]
        public IFormFile? IdProofImage { get; set; }
        
        public string? ExistingIdProofUrl { get; set; }
        
        public bool IsVerified { get; set; }
    }
}