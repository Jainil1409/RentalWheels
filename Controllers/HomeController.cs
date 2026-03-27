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

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Find bookings that are Approved but don't have a completed payment yet
                    var approvedBookings = await _context.Bookings
                        .Include(b => b.Vehicle)
                        .Where(b => b.CustomerId == userId 
                            && b.Status == BookingStatus.Approved 
                            && !_context.Payments.Any(p => p.BookingId == b.Id && p.Status == PaymentStatus.Completed))
                        .ToListAsync();
                        
                    ViewBag.ApprovedBookingsToPay = approvedBookings;
                }
            }

            return View(vehicles);
        }

        public async Task<IActionResult> Notifications()
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Customer"))
            {
                return RedirectToAction("Login", "Account");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Index");
            }

            var currentUser = await _context.Users.FindAsync(userId);
            ViewBag.CurrentUser = currentUser;

            var approvedBookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Where(b => b.CustomerId == userId 
                    && b.Status == BookingStatus.Approved 
                    && !_context.Payments.Any(p => p.BookingId == b.Id && p.Status == PaymentStatus.Completed))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
                
            var damageReports = await _context.DamageReports
                .Include(dr => dr.Booking)
                .ThenInclude(b => b!.Vehicle)
                .Where(dr => dr.Booking != null && dr.Booking.CustomerId == userId && !dr.IsPaid)
                .OrderByDescending(dr => dr.CreatedAt)
                .ToListAsync();

            var depositRefundNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.Type == "DepositRefund")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Mark deposit refund notifications as read
            foreach (var n in depositRefundNotifications.Where(n => !n.IsRead))
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();

            ViewBag.DamageReports = damageReports;
            ViewBag.DepositRefundNotifications = depositRefundNotifications;

            return View(approvedBookings);
        }

        public async Task<IActionResult> AdminNotifications()
        {
            if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Admin"))
            {
                return RedirectToAction("Login", "Account");
            }

            var pendingBookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .Where(b => b.Status == BookingStatus.Pending)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var recentPayments = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Customer)
                .Where(p => p.Status == PaymentStatus.Completed)
                .OrderByDescending(p => p.PaymentDate)
                .Take(10)
                .ToListAsync();

            var damagePayments = await _context.DamageReports
                .Include(dr => dr.Booking)
                .ThenInclude(b => b!.Customer)
                .Where(dr => dr.IsPaid)
                .OrderByDescending(dr => dr.CreatedAt)
                .Take(10)
                .ToListAsync();

            var pendingVerifications = await _context.Users
                .Where(u => u.Role == UserRole.Customer && !string.IsNullOrEmpty(u.DriverLicenseNumber) && !u.IsVerified)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.PendingBookings = pendingBookings;
            ViewBag.RecentPayments = recentPayments;
            ViewBag.DamagePayments = damagePayments;
            ViewBag.PendingVerifications = pendingVerifications;

            // Mark admin notifications as read when viewed
            var unreadNotifications = await _context.Notifications
                .Where(n => !n.IsRead && n.UserId == null)
                .ToListAsync();
            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }
            if (unreadNotifications.Any())
            {
                await _context.SaveChangesAsync();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AboutUs()
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
