using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Models;
using vehicle_management_system_mvc.ViewModels;

namespace vehicle_management_system_mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                var dashboard = new DashboardViewModel
                {
                    TotalVehicles = await _context.Vehicles.CountAsync(),
                    AvailableVehicles = await _context.Vehicles.CountAsync(v => v.IsAvailable),
                    TotalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                    ActiveRentals = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Approved),
                    PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
                    OverdueRentals = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Approved && b.EndDate < DateTime.UtcNow),
                    TotalRevenue = await _context.Payments.Where(p => p.Status == PaymentStatus.Completed).SumAsync(p => p.Amount)
                };
                return View("AdminDashboard", dashboard);
            }

            var vehicles = await _context.Vehicles
                .Where(v => v.IsAvailable)
                .OrderBy(v => v.Brand)
                .Take(6)
                .ToListAsync();

            return View(vehicles);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
