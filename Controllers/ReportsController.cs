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

        public async Task<IActionResult> VehicleUsage(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var vehiclesQuery = _context.Vehicles
                .Include(v => v.Bookings)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                vehiclesQuery = vehiclesQuery.Where(v => v.Brand.Contains(searchString) ||
                                                         v.Model.Contains(searchString) ||
                                                         v.Type.ToString().Contains(searchString));
            }

            vehiclesQuery = vehiclesQuery.OrderByDescending(v => v.Bookings.Count);

            int pageSize = 10;
            return View(await vehicle_management_system_mvc.Helpers.PaginatedList<Vehicle>.CreateAsync(vehiclesQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> RentalHistory(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var bookingsQuery = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Customer.FullName.Contains(searchString) ||
                                                         b.Vehicle.Brand.Contains(searchString) ||
                                                         b.Vehicle.Model.Contains(searchString) ||
                                                         b.Id.ToString().Contains(searchString));
            }

            bookingsQuery = bookingsQuery.OrderByDescending(b => b.CreatedAt);
            
            int pageSize = 10;
            return View(await vehicle_management_system_mvc.Helpers.PaginatedList<Booking>.CreateAsync(bookingsQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        public async Task<IActionResult> PaymentReport(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var paymentsQuery = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Vehicle)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                paymentsQuery = paymentsQuery.Where(p => (p.Booking.Customer.FullName != null && p.Booking.Customer.FullName.Contains(searchString)) ||
                                                         (p.Booking.Vehicle.Brand != null && p.Booking.Vehicle.Brand.Contains(searchString)) ||
                                                         (p.Booking.Vehicle.Model != null && p.Booking.Vehicle.Model.Contains(searchString)) ||
                                                         (p.StripePaymentIntentId != null && p.StripePaymentIntentId.Contains(searchString)) ||
                                                         (p.InvoiceNumber != null && p.InvoiceNumber.Contains(searchString)) ||
                                                         p.Id.ToString().Contains(searchString));
            }

            paymentsQuery = paymentsQuery.OrderByDescending(p => p.PaymentDate);
            
            int pageSize = 10;
            return View(await vehicle_management_system_mvc.Helpers.PaginatedList<Payment>.CreateAsync(paymentsQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
    }
}


