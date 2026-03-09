using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Models;
using vehicle_management_system_mvc.ViewModels;

namespace vehicle_management_system_mvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                TotalVehicles = await _context.Vehicles.CountAsync(),
                AvailableVehicles = await _context.Vehicles.CountAsync(v => v.IsAvailable),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                ActiveRentals = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Approved),
                PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
                OverdueRentals = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Approved && b.EndDate < DateTime.UtcNow),
                TotalRevenue = await _context.Payments.Where(p => p.Status == PaymentStatus.Completed).SumAsync(p => p.Amount)
            };

            return View(model);
        }

        public async Task<IActionResult> VehicleUsage()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Bookings)
                .OrderByDescending(v => v.Bookings.Count)
                .ToListAsync();

            return View(vehicles);
        }

        public async Task<IActionResult> RentalHistory()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> PaymentReport()
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Vehicle)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }
    }
}
