using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Models;
using vehicle_management_system_mvc.ViewModels;

namespace vehicle_management_system_mvc.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin() => User.IsInRole("Admin");

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null || !vehicle.IsAvailable) return NotFound();

            var model = new BookingCreateViewModel
            {
                VehicleId = vehicleId,
                Vehicle = vehicle,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            var vehicle = await _context.Vehicles.FindAsync(model.VehicleId);
            if (vehicle == null || !vehicle.IsAvailable)
            {
                ModelState.AddModelError(string.Empty, "Vehicle is not available.");
                model.Vehicle = vehicle;
                return View(model);
            }

            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
                model.Vehicle = vehicle;
                return View(model);
            }

            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
                model.Vehicle = vehicle;
                return View(model);
            }

            var days = (model.EndDate - model.StartDate).Days;
            var totalCost = days * vehicle.PricePerDay;

            var booking = new Booking
            {
                CustomerId = GetUserId(),
                VehicleId = model.VehicleId,
                StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc),
                TotalCost = totalCost,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking created! Total cost: ₹{totalCost:N2} for {days} day(s).";
            return RedirectToAction(nameof(MyBookings));
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyBookings()
        {
            var userId = GetUserId();
            var bookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Approved;
            booking.Vehicle.IsAvailable = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking approved.";
            return RedirectToAction(nameof(AllBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking rejected.";
            return RedirectToAction(nameof(AllBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Complete(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Completed;
            booking.Vehicle.IsAvailable = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rental marked as completed. Vehicle is now available.";
            return RedirectToAction(nameof(AllBookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = GetUserId();
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == userId);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "Only pending bookings can be cancelled.";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled.";
            return RedirectToAction(nameof(MyBookings));
        }
    }
}
