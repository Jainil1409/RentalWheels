using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace vehicle_management_system_mvc.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Please enter a valid 10-digit phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // KYC & Profile Information
        [Required(ErrorMessage = "Driver's License Number is required.")]
        [Display(Name = "Driver's License Number")]
        public string DriverLicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License Expiry Date is required.")]
        [Display(Name = "License Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; }

        [Required(ErrorMessage = "Residential Address is required.")]
        [Display(Name = "Residential Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please upload a copy of your Driver's License/ID.")]
        [Display(Name = "Upload Driver's License/ID")]
        public IFormFile? IdProofFile { get; set; }
    }
}
