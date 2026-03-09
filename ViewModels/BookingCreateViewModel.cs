using System.ComponentModel.DataAnnotations;
using vehicle_management_system_mvc.Models;

namespace vehicle_management_system_mvc.ViewModels
{
    public class BookingCreateViewModel
    {
        [Required]
        public int VehicleId { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public Vehicle? Vehicle { get; set; }
    }
}
