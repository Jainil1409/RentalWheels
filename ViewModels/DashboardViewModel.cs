namespace vehicle_management_system_mvc.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveRentals { get; set; }
        public int PendingBookings { get; set; }
        public int OverdueRentals { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
