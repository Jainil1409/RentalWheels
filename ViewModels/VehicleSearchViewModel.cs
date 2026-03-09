using vehicle_management_system_mvc.Models;

namespace vehicle_management_system_mvc.ViewModels
{
    public class VehicleSearchViewModel
    {
        public string? Brand { get; set; }
        public VehicleType? Type { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool AvailableOnly { get; set; }

        public List<Vehicle> Vehicles { get; set; } = [];
    }
}
