using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace vehicle_management_system_mvc.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendInvoiceEmailAsync(
            string toEmail,
            string customerName,
            string invoiceNumber,
            string vehicleName,
            string vehicleDetails,
            string rentalPeriod,
            int days,
            decimal ratePerDay,
            decimal totalAmount,
            string paymentMethod,
            DateTime paymentDate,
            string? stripeRef = null)
        {
            var smtpHost = _configuration["Email:SmtpHost"]!;
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"]!);
            var senderEmail = _configuration["Email:SenderEmail"]!;
            var senderPassword = _configuration["Email:SenderPassword"]!;
            var senderName = _configuration["Email:SenderName"] ?? "RentWheels";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(customerName, toEmail));
            message.Subject = $"RentWheels Invoice {invoiceNumber} — Payment Confirmation";

            var htmlBody = GenerateInvoiceHtml(
                customerName, toEmail, invoiceNumber, vehicleName, vehicleDetails,
                rentalPeriod, days, ratePerDay, totalAmount, paymentMethod, paymentDate, stripeRef);

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string GenerateInvoiceHtml(
            string customerName, string customerEmail, string invoiceNumber,
            string vehicleName, string vehicleDetails, string rentalPeriod,
            int days, decimal ratePerDay, decimal totalAmount,
            string paymentMethod, DateTime paymentDate, string? stripeRef)
        {
            var methodBadge = paymentMethod == "Stripe"
                ? "<span style=\"background:#0061f2;color:#fff;padding:4px 12px;border-radius:20px;font-size:13px;\">💳 Stripe</span>"
                : "<span style=\"background:#10b981;color:#fff;padding:4px 12px;border-radius:20px;font-size:13px;\">💵 Cash</span>";

            var stripeSection = !string.IsNullOrEmpty(stripeRef)
                ? $"<tr><td style=\"padding:8px 0;color:#6b7280;font-size:13px;\">Stripe Reference</td><td style=\"padding:8px 0;text-align:right;font-size:13px;color:#374151;\">{stripeRef}</td></tr>"
                : "";

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8"" /><meta name=""viewport"" content=""width=device-width,initial-scale=1"" /></head>
<body style=""margin:0;padding:0;background:#f3f4f6;font-family:'Segoe UI',Roboto,Arial,sans-serif;"">
<div style=""max-width:600px;margin:0 auto;padding:24px;"">

  <!-- Header -->
  <div style=""background:linear-gradient(135deg,#0061f2,#6610f2);border-radius:16px 16px 0 0;padding:32px;text-align:center;"">
    <h1 style=""color:#fff;margin:0;font-size:28px;"">🚗 RentWheels</h1>
    <p style=""color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:14px;"">Payment Confirmation & Invoice</p>
  </div>

  <!-- Body -->
  <div style=""background:#fff;padding:32px;border-radius:0 0 16px 16px;box-shadow:0 4px 20px rgba(0,0,0,0.08);"">
    
    <!-- Greeting -->
    <p style=""font-size:16px;color:#1f2937;margin:0 0 4px;"">Hello <strong>{customerName}</strong>,</p>
    <p style=""font-size:14px;color:#6b7280;margin:0 0 24px;"">Your payment has been processed successfully. Here's your invoice:</p>

    <!-- Invoice Number -->
    <div style=""background:#f0f5ff;border:1px solid #dbeafe;border-radius:12px;padding:16px;text-align:center;margin-bottom:24px;"">
      <span style=""font-size:12px;text-transform:uppercase;letter-spacing:1px;color:#6b7280;"">Invoice Number</span><br/>
      <strong style=""font-size:20px;color:#0061f2;"">{invoiceNumber}</strong>
    </div>

    <!-- Customer & Payment Info -->
    <table style=""width:100%;margin-bottom:24px;"">
      <tr>
        <td style=""vertical-align:top;width:50%;"">
          <span style=""font-size:11px;text-transform:uppercase;letter-spacing:0.5px;color:#9ca3af;"">Billed To</span><br/>
          <strong style=""font-size:14px;color:#1f2937;"">{customerName}</strong><br/>
          <span style=""font-size:13px;color:#6b7280;"">{customerEmail}</span>
        </td>
        <td style=""vertical-align:top;width:50%;text-align:right;"">
          <span style=""font-size:11px;text-transform:uppercase;letter-spacing:0.5px;color:#9ca3af;"">Payment Date</span><br/>
          <strong style=""font-size:14px;color:#1f2937;"">{paymentDate:MMMM dd, yyyy}</strong><br/>
          {methodBadge}
        </td>
      </tr>
    </table>

    <!-- Divider -->
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:0 0 24px;"" />

    <!-- Rental Details -->
    <table style=""width:100%;border-collapse:collapse;"">
      <tr style=""background:#f9fafb;"">
        <th style=""text-align:left;padding:12px;font-size:12px;text-transform:uppercase;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Description</th>
        <th style=""text-align:center;padding:12px;font-size:12px;text-transform:uppercase;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Days</th>
        <th style=""text-align:right;padding:12px;font-size:12px;text-transform:uppercase;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Rate/Day</th>
        <th style=""text-align:right;padding:12px;font-size:12px;text-transform:uppercase;color:#6b7280;border-bottom:2px solid #e5e7eb;"">Amount</th>
      </tr>
      <tr>
        <td style=""padding:16px 12px;"">
          <strong style=""font-size:14px;color:#1f2937;"">{vehicleName}</strong><br/>
          <span style=""font-size:12px;color:#9ca3af;"">{vehicleDetails}</span><br/>
          <span style=""font-size:12px;color:#9ca3af;"">{rentalPeriod}</span>
        </td>
        <td style=""padding:16px 12px;text-align:center;font-size:14px;color:#374151;"">{days}</td>
        <td style=""padding:16px 12px;text-align:right;font-size:14px;color:#374151;"">₹{ratePerDay:N2}</td>
        <td style=""padding:16px 12px;text-align:right;font-size:14px;font-weight:700;color:#1f2937;"">₹{totalAmount:N2}</td>
      </tr>
    </table>

    <!-- Totals -->
    <div style=""background:#f9fafb;border-radius:12px;padding:16px;margin-top:16px;"">
      <table style=""width:100%;max-width:280px;margin-left:auto;"">
        <tr>
          <td style=""padding:6px 0;color:#6b7280;font-size:14px;"">Subtotal</td>
          <td style=""padding:6px 0;text-align:right;font-size:14px;color:#374151;"">₹{totalAmount:N2}</td>
        </tr>
        <tr>
          <td style=""padding:6px 0;color:#6b7280;font-size:14px;"">Tax</td>
          <td style=""padding:6px 0;text-align:right;font-size:14px;color:#374151;"">₹0.00</td>
        </tr>
        {stripeSection}
        <tr>
          <td colspan=""2""><hr style=""border:none;border-top:2px solid #e5e7eb;margin:8px 0;"" /></td>
        </tr>
        <tr>
          <td style=""padding:6px 0;font-size:18px;font-weight:700;color:#1f2937;"">Total Paid</td>
          <td style=""padding:6px 0;text-align:right;font-size:18px;font-weight:700;color:#10b981;"">₹{totalAmount:N2}</td>
        </tr>
      </table>
    </div>

    <!-- Status Badge -->
    <div style=""text-align:center;margin-top:24px;"">
      <span style=""background:#d1fae5;color:#059669;padding:8px 20px;border-radius:50px;font-weight:600;font-size:14px;"">
        ✅ Payment Completed
      </span>
    </div>

    <!-- Divider -->
    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0;"" />

    <!-- Footer Note -->
    <p style=""text-align:center;font-size:13px;color:#9ca3af;margin:0;"">
      Thank you for choosing <strong>RentWheels</strong>!<br/>
      For any queries, contact <a href=""mailto:support@rentwheels.com"" style=""color:#0061f2;text-decoration:none;"">support&#64;rentwheels.com</a>
    </p>
  </div>

  <!-- Email Disclaimer -->
  <p style=""text-align:center;font-size:11px;color:#9ca3af;margin-top:16px;"">
    This is an automated email. Please do not reply directly to this message.
  </p>

</div>
</body>
</html>";
        }
    }
}