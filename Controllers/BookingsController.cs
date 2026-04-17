using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Models;
using vehicle_management_system_mvc.Services;
using vehicle_management_system_mvc.ViewModels;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace vehicle_management_system_mvc.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public BookingsController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin() => User.IsInRole("Admin");

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int vehicleId)
        {
            var currentUser = await _context.Users.FindAsync(GetUserId());
            if (string.IsNullOrEmpty(currentUser?.DriverLicenseNumber) || string.IsNullOrEmpty(currentUser?.IdProofUrl))
            {
                TempData["Warning"] = "Please complete your driver profile and upload your license image before booking a vehicle.";
                return RedirectToAction("Profile", "Account", new { returnUrl = Url.Action("Create", "Bookings", new { vehicleId = vehicleId }) });
            }
            
            if (!currentUser.IsVerified)
            {
                TempData["Warning"] = "Your KYC profile is under review. You cannot book vehicles until the Admin verifies your documents.";
                return RedirectToAction("Profile", "Account");
            }

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
            var currentUser = await _context.Users.FindAsync(GetUserId());
            if (string.IsNullOrEmpty(currentUser?.DriverLicenseNumber) || string.IsNullOrEmpty(currentUser?.IdProofUrl))
            {
                TempData["Warning"] = "Please complete your driver profile and upload your license image first.";
                return RedirectToAction("Profile", "Account", new { returnUrl = Url.Action("Create", "Bookings", new { vehicleId = model.VehicleId }) });
            }
            
            if (!currentUser.IsVerified)
            {
                TempData["Warning"] = "Your KYC profile is under review. You cannot book vehicles until the Admin verifies your documents.";
                return RedirectToAction("Profile", "Account");
            }

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
            var depositAmount = vehicle.DepositAmount;

            var booking = new Booking
            {
                CustomerId = GetUserId(),
                VehicleId = model.VehicleId,
                StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc),
                TotalCost = totalCost,
                DepositAmount = depositAmount,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking created! Rent: ₹{totalCost:N2} & Deposit: ₹{depositAmount:N2} for {days} day(s).";
            return RedirectToAction(nameof(MyBookings));
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyBookings()
        {
            var userId = GetUserId();
            var bookings = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .Include(b => b.DamageReport)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllBookings(string searchString, int? pageNumber)
        {
            if (searchString != null)
            {
                pageNumber = 1;
            }

            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => 
                    b.Customer!.FullName.Contains(searchString) || 
                    b.Vehicle!.Brand.Contains(searchString) || 
                    b.Vehicle!.Model.Contains(searchString) ||
                    b.Status.ToString().Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            int pageSize = 10;
            var bookings = await vehicle_management_system_mvc.Helpers.PaginatedList<Booking>.CreateAsync(
                query.OrderByDescending(b => b.CreatedAt), pageNumber ?? 1, pageSize);

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
        public IActionResult Complete(int id)
        {
            // Redirect to DamageReport generation
            return RedirectToAction(nameof(DamageReport), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CompleteWithoutDamage(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Completed;
            booking.Vehicle.IsAvailable = true;
            
            booking.DepositDeducted = 0;
            booking.DepositRefunded = booking.DepositAmount;
            booking.IsDepositRefunded = true;

            // Refund deposit via Stripe if payment was made via Stripe
            string? stripeRefundId = null;
            if (booking.Payment?.Method == vehicle_management_system_mvc.Models.PaymentMethod.Stripe && !string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId) && booking.DepositAmount > 0)
            {
                stripeRefundId = await RefundDepositViaStripeAsync(booking.Payment.StripePaymentIntentId, booking.DepositAmount);
                booking.StripeDepositRefundId = stripeRefundId;
            }

            // User-facing notification for deposit refund
            var userNotification = new Notification
            {
                Message = $"Your security deposit of ₹{booking.DepositAmount:N2} for Booking #{booking.Id} has been refunded to your original payment method.",
                Type = "DepositRefund",
                BookingId = booking.Id,
                UserId = booking.CustomerId
            };
            _context.Notifications.Add(userNotification);

            await _context.SaveChangesAsync();

            // Send refund email to user
            await SendDepositRefundEmailSafeAsync(booking, 0, booking.DepositAmount, stripeRefundId);

            TempData["Success"] = "Rental completed cleanly with no damage. Deposit fully refunded. Vehicle is now available.";

            return RedirectToAction(nameof(AllBookings));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DamageReport(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewBag.DepositAmount = booking.DepositAmount;
            var newReport = new DamageReport { BookingId = id, DamageCost = 0 };
            return View(newReport);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateDamageReport(DamageReport model, bool skip = false)
        {
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);
            if (booking == null) return NotFound();

            // Mark booking as completed and vehicle available
            booking.Status = BookingStatus.Completed;
            booking.Vehicle.IsAvailable = true;

            decimal damageCost = skip ? 0 : model.DamageCost;
            
            // Deduct from deposit
            if (damageCost <= booking.DepositAmount)
            {
                booking.DepositDeducted = damageCost;
                booking.DepositRefunded = booking.DepositAmount - damageCost;
                booking.IsDepositRefunded = true; // Refunded remainder
            }
            else
            {
                booking.DepositDeducted = booking.DepositAmount;
                booking.DepositRefunded = 0;
                booking.IsDepositRefunded = true; // Fully consumed
            }

            string? stripeRefundId = null;

            if (!skip && model.DamageCost > 0)
            {
                var report = new DamageReport
                {
                    BookingId = model.BookingId,
                    Description = model.Description ?? "No specific details provided",
                    DamageCost = model.DamageCost,
                    IsPaid = damageCost <= booking.DepositAmount, // Paid via deposit if covered
                    CreatedAt = DateTime.UtcNow,
                    PdfUrl = "" // Updated after save
                };

                _context.DamageReports.Add(report);

                // Refund remaining deposit via Stripe if applicable
                if (booking.DepositRefunded > 0 && booking.Payment?.Method == vehicle_management_system_mvc.Models.PaymentMethod.Stripe && !string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId))
                {
                    stripeRefundId = await RefundDepositViaStripeAsync(booking.Payment.StripePaymentIntentId, booking.DepositRefunded);
                    booking.StripeDepositRefundId = stripeRefundId;
                }

                // User-facing notification for deposit refund (even if 0, to inform about damage deduction)
                if (booking.DepositRefunded > 0)
                {
                    var userNotification = new Notification
                    {
                        Message = $"Your security deposit of ₹{booking.DepositAmount:N2} for Booking #{booking.Id} had ₹{damageCost:N2} deducted for damages. ₹{booking.DepositRefunded:N2} has been refunded.",
                        Type = "DepositRefund",
                        BookingId = booking.Id,
                        UserId = booking.CustomerId
                    };
                    _context.Notifications.Add(userNotification);
                }

                await _context.SaveChangesAsync();
                
                report.PdfUrl = $"/Bookings/DownloadDamagePdf/{report.Id}";
                await _context.SaveChangesAsync();

                // Send refund email if deposit was partially/fully refunded
                if (booking.DepositRefunded > 0)
                {
                    await SendDepositRefundEmailSafeAsync(booking, damageCost, booking.DepositRefunded, stripeRefundId, report.PdfUrl);
                }

                TempData["Success"] = $"Damage report generated!. Cost: ₹{model.DamageCost:N2}. Vehicle is now available.";
            }
            else
            {
                // No damage — full deposit refund
                if (booking.DepositRefunded > 0 && booking.Payment?.Method == vehicle_management_system_mvc.Models.PaymentMethod.Stripe && !string.IsNullOrEmpty(booking.Payment.StripePaymentIntentId))
                {
                    stripeRefundId = await RefundDepositViaStripeAsync(booking.Payment.StripePaymentIntentId, booking.DepositRefunded);
                    booking.StripeDepositRefundId = stripeRefundId;
                }

                var userNotification = new Notification
                {
                    Message = $"Your security deposit of ₹{booking.DepositAmount:N2} for Booking #{booking.Id} has been fully refunded to your original payment method.",
                    Type = "DepositRefund",
                    BookingId = booking.Id,
                    UserId = booking.CustomerId
                };
                _context.Notifications.Add(userNotification);

                await _context.SaveChangesAsync();

                await SendDepositRefundEmailSafeAsync(booking, 0, booking.DepositRefunded, stripeRefundId);

                TempData["Success"] = "Rental completed cleanly with no extra damage charges. Vehicle is now available.";
            }

            return RedirectToAction(nameof(AllBookings));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDamagePdf(int id)
        {
            var report = await _context.DamageReports
                .Include(r => r.Booking)
                .ThenInclude(b => b!.Vehicle)
                .Include(r => r.Booking!.Customer)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return NotFound();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(compose =>
                    {
                        compose.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("RentWheels Damage Report").FontSize(20).SemiBold().FontColor(Colors.Red.Medium);
                                col.Item().Text($"Report ID: {report.Id}");
                                col.Item().Text($"Date: {report.CreatedAt:MMM dd, yyyy}");
                            });
                        });
                    });

                    page.Content().Element(compose =>
                    {
                        compose.PaddingVertical(1, Unit.Centimetre).Column(column =>
                        {
                            column.Item().Text("Customer Details").SemiBold();
                            column.Item().Text($"Name: {report.Booking?.Customer?.FullName}");
                            column.Item().Text($"Email: {report.Booking?.Customer?.Email}");

                            column.Item().PaddingTop(10).Text("Vehicle Details").SemiBold();
                            column.Item().Text($"Vehicle: {report.Booking?.Vehicle?.Brand} {report.Booking?.Vehicle?.Model}");
                            column.Item().Text($"License Plate: {report.Booking?.Vehicle?.LicensePlate}");

                            column.Item().PaddingTop(10).Text("Damage Details").SemiBold();
                            column.Item().Text(report.Description);

                            column.Item().PaddingTop(20).Text($"Estimated Cost: Rs. {report.DamageCost:N2}").FontSize(16).SemiBold().FontColor(Colors.Red.Medium);
                            
                            column.Item().PaddingTop(10).Text($"Payment Status: {(report.IsPaid ? "PAID" : "PENDING")}").Bold();
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"DamageReport_{report.Id}.pdf");
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

        // --- Helper Methods ---

        private async Task<string?> RefundDepositViaStripeAsync(string paymentIntentId, decimal refundAmount)
        {
            try
            {
                var refundService = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Amount = (long)(refundAmount * 100), // Stripe uses cents/paise
                    Reason = "requested_by_customer"
                };
                var refund = await refundService.CreateAsync(refundOptions);
                return refund.Id;
            }
            catch (Exception)
            {
                // Stripe refund failure should not block the completion flow
                return null;
            }
        }

        private async Task SendDepositRefundEmailSafeAsync(Booking booking, decimal damageCost, decimal refundAmount, string? stripeRefundId, string? pdfUrl = null)
        {
            try
            {
                if (booking.Customer == null || booking.Vehicle == null) return;
                
                string? absolutePdfUrl = null;
                if (!string.IsNullOrEmpty(pdfUrl))
                {
                    absolutePdfUrl = $"{Request.Scheme}://{Request.Host}{pdfUrl}";
                }

                await _emailService.SendDepositRefundEmailAsync(
                    toEmail: booking.Customer.Email,
                    customerName: booking.Customer.FullName,
                    vehicleName: $"{booking.Vehicle.Brand} {booking.Vehicle.Model}",
                    bookingId: booking.Id,
                    depositAmount: booking.DepositAmount,
                    damageCost: damageCost,
                    refundAmount: refundAmount,
                    stripeRefundId: stripeRefundId,
                    damagePdfLink: absolutePdfUrl);
            }
            catch
            {
                // Email failure should not block the flow
            }
        }
    }
}

