using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Models;
using vehicle_management_system_mvc.Services;
using vehicle_management_system_mvc.ViewModels;

namespace vehicle_management_system_mvc.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public PaymentsController(ApplicationDbContext context, IConfiguration configuration, EmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task SendInvoiceEmailAsync(Booking booking, Payment payment)
        {
            try
            {
                var customer = await _context.Users.FindAsync(booking.CustomerId);
                if (customer == null) return;

                var days = (booking.EndDate - booking.StartDate).Days;
                var vehicle = booking.Vehicle;

                await _emailService.SendInvoiceEmailAsync(
                    toEmail: customer.Email,
                    customerName: customer.FullName,
                    invoiceNumber: payment.InvoiceNumber ?? "N/A",
                    vehicleName: $"{vehicle.Brand} {vehicle.Model}",
                    vehicleDetails: $"{vehicle.Type} • {vehicle.Year} • {vehicle.LicensePlate}",
                    rentalPeriod: $"{booking.StartDate:MMM dd, yyyy} — {booking.EndDate:MMM dd, yyyy}",
                    days: days,
                    ratePerDay: vehicle.PricePerDay,
                    totalAmount: payment.Amount,
                    paymentMethod: payment.Method.ToString(),
                    paymentDate: payment.PaymentDate,
                    stripeRef: payment.StripePaymentIntentId);
            }
            catch
            {
                // Email failure should not block the payment flow
            }
        }

        private string GenerateInvoiceNumber()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random();
            var randomPart = random.Next(1000, 9999);
            return $"INV-{datePart}-{randomPart}";
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = GetUserId();
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Approved)
            {
                TempData["Error"] = "Payment can only be made for approved bookings.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (booking.Payment != null)
            {
                TempData["Error"] = "Payment has already been made for this booking.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            var model = new PaymentCreateViewModel
            {
                BookingId = bookingId,
                Booking = booking,
                StripePublishableKey = _configuration["Stripe:PublishableKey"] ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(PaymentCreateViewModel model)
        {
            var userId = GetUserId();
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.CustomerId == userId);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Approved || booking.Payment != null)
            {
                TempData["Error"] = "Payment cannot be processed.";
                return RedirectToAction("MyBookings", "Bookings");
            }

            if (model.Method == PaymentMethod.Cash)
            {
                var paymentAmount = booking.TotalCost + booking.DepositAmount;
                var payment = new Payment
                {
                    BookingId = model.BookingId,
                    Amount = paymentAmount,
                    PaymentDate = DateTime.UtcNow,
                    Method = PaymentMethod.Cash,
                    Status = PaymentStatus.Completed,
                    InvoiceNumber = GenerateInvoiceNumber()
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Send invoice email
                await SendInvoiceEmailAsync(booking, payment);

                TempData["Success"] = $"Cash payment of ₹{paymentAmount:N2} completed successfully.";
                return RedirectToAction("Invoice", new { paymentId = payment.Id });
            }

            // Stripe Checkout Session
            var domain = $"{Request.Scheme}://{Request.Host}";
            var totalWithDeposit = booking.TotalCost + booking.DepositAmount;
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmountDecimal = totalWithDeposit * 100, // Stripe uses cents
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{booking.Vehicle.Brand} {booking.Vehicle.Model} Rental",
                                Description = $"Booking #{booking.Id} — Rental + Deposit"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    SetupFutureUsage = "off_session"
                },
                SuccessUrl = $"{domain}/Payments/StripeSuccess?bookingId={booking.Id}&sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Payments/Create?bookingId={booking.Id}",
                Metadata = new Dictionary<string, string>
                {
                    { "bookingId", booking.Id.ToString() },
                    { "userId", userId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> StripeSuccess(int bookingId, string sessionId)
        {
            var userId = GetUserId();
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);

            if (booking == null) return NotFound();

            if (booking.Payment != null)
            {
                return RedirectToAction("Invoice", new { paymentId = booking.Payment.Id });
            }

            // Verify the Stripe session
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            if (session.PaymentStatus != "paid")
            {
                TempData["Error"] = "Payment was not completed. Please try again.";
                return RedirectToAction("Create", new { bookingId });
            }

            var totalWithDeposit = booking.TotalCost + booking.DepositAmount;
            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = totalWithDeposit,
                PaymentDate = DateTime.UtcNow,
                Method = PaymentMethod.Stripe,
                Status = PaymentStatus.Completed,
                StripePaymentIntentId = session.PaymentIntentId,
                InvoiceNumber = GenerateInvoiceNumber()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Send invoice email
            await SendInvoiceEmailAsync(booking, payment);

            TempData["Success"] = $"Stripe payment of ₹{totalWithDeposit:N2} completed successfully.";
            return RedirectToAction("Invoice", new { paymentId = payment.Id });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PayDamage(int reportId)
        {
            var report = await _context.DamageReports
                .Include(r => r.Booking)
                .ThenInclude(b => b!.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null) return NotFound();

            var userId = GetUserId();
            if (report.Booking != null && report.Booking.CustomerId != userId) return NotFound();

            return View(report);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDamagePayment(int reportId, string paymentMethod)
        {
            var userId = GetUserId();
            var report = await _context.DamageReports
                .Include(r => r.Booking)
                .ThenInclude(b => b!.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.Booking != null && r.Booking.CustomerId == userId);

            if (report == null || report.IsPaid) return NotFound();

            if (paymentMethod == "Cash")
            {
                report.IsPaid = true;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Cash payment of ₹{report.DamageCost:N2} for damage penalty completed successfully.";
                return RedirectToAction("DamageInvoice", new { reportId = report.Id });
            }

            // Stripe Checkout Session for Damage Report
            var domain = $"{Request.Scheme}://{Request.Host}";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmountDecimal = report.DamageCost * 100, // Stripe uses cents
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Damage Penalty: {report.Booking?.Vehicle?.Brand} {report.Booking?.Vehicle?.Model}",
                                Description = $"Damage Report #{report.Id} - {report.Description}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{domain}/Payments/StripeSuccessDamage?reportId={report.Id}&sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/Payments/PayDamage?reportId={report.Id}",
                Metadata = new Dictionary<string, string>
                {
                    { "reportId", report.Id.ToString() },
                    { "userId", userId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> StripeSuccessDamage(int reportId, string sessionId)
        {
            var userId = GetUserId();
            var report = await _context.DamageReports
                .Include(r => r.Booking)
                .ThenInclude(b => b!.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.Booking != null && r.Booking.CustomerId == userId);

            if (report == null) return NotFound();

            if (report.IsPaid)
            {
                return RedirectToAction("DamageInvoice", new { reportId = report.Id });
            }

            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            if (session.PaymentStatus != "paid")
            {
                TempData["Error"] = "Payment was not completed. Please try again.";
                return RedirectToAction("PayDamage", new { reportId });
            }

            report.IsPaid = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Stripe payment of ₹{report.DamageCost:N2} for damage penalty completed successfully.";
            return RedirectToAction("DamageInvoice", new { reportId = report.Id });
        }

        [Authorize]
        public async Task<IActionResult> DamageInvoice(int reportId)
        {
            var report = await _context.DamageReports
                .Include(r => r.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(r => r.Booking)
                    .ThenInclude(b => b!.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null) return NotFound();

            if (User.IsInRole("Customer"))
            {
                var userId = GetUserId();
                if (report.Booking != null && report.Booking.CustomerId != userId) return NotFound();
            }

            return View(report);
        }

        [Authorize]
        public async Task<IActionResult> Invoice(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Vehicle)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return NotFound();

            // Customers can only see their own invoices
            if (User.IsInRole("Customer"))
            {
                var userId = GetUserId();
                if (payment.Booking != null && payment.Booking.CustomerId != userId) return NotFound();
            }

            return View(payment);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Vehicle)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var damagePayments = await _context.DamageReports
                .Include(r => r.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(r => r.Booking)
                    .ThenInclude(b => b!.Vehicle)
                .Where(r => r.IsPaid)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.DamagePayments = damagePayments;

            return View(payments);
        }
    }
}
