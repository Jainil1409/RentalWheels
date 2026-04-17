using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MailKit;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace vehicle_management_system_mvc.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private async Task SendMessageAsync(MimeMessage message)
        {
            var smtpHost = _configuration["Email:SmtpHost"]!;
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"]!);
            var smtpUsername = (_configuration["Email:SmtpUsername"] ?? _configuration["Email:SenderEmail"] ?? string.Empty).Trim();
            var senderPasswordRaw = _configuration["Email:SenderPassword"] ?? string.Empty;
            var senderPassword = senderPasswordRaw.Replace(" ", string.Empty).Trim();
            var disableCertRevocationCheck = bool.TryParse(_configuration["Email:DisableCertificateRevocationCheck"], out var parsedValue) && parsedValue;

            if (string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(senderPassword))
            {
                throw new InvalidOperationException("Email SMTP credentials are missing. Set Email:SmtpUsername (or Email:SenderEmail) and Email:SenderPassword.");
            }

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = !disableCertRevocationCheck;

            try
            {
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUsername, senderPassword);
                await client.SendAsync(message);
            }
            catch (AuthenticationException ex)
            {
                throw new InvalidOperationException(
                    "Gmail SMTP authentication failed. Verify 2-Step Verification is ON and use a fresh 16-character App Password for Email:SenderPassword.", ex);
            }
            catch (SmtpCommandException ex) when (ex.Message.Contains("5.7.8", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Gmail rejected SMTP credentials (5.7.8 BadCredentials). Regenerate App Password and update Email:SenderPassword.", ex);
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }

        public async Task SendInvoiceEmailAsync(
            string customerName, string toEmail, string invoiceNumber,
            string vehicleName, string vehicleDetails, string rentalPeriod,
            int days, decimal ratePerDay, decimal depositAmount, decimal totalAmount,
            string paymentMethod, DateTime paymentDate, string? stripeRef = null)
        {
            var senderEmail = (_configuration["Email:SenderEmail"] ?? string.Empty).Trim();
            var senderName = _configuration["Email:SenderName"] ?? "RentWheels";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(customerName, toEmail));
            message.Subject = $"RentWheels Invoice {invoiceNumber} — Payment Confirmation";

            // Send invoice as attachment-only email (no HTML invoice body).
            var bodyBuilder = new BodyBuilder();
            var pdfBytes = GenerateInvoicePdf(customerName, toEmail, invoiceNumber, vehicleName, vehicleDetails, rentalPeriod, days, ratePerDay, depositAmount, totalAmount, paymentMethod, paymentDate, stripeRef);
            bodyBuilder.Attachments.Clear();
            bodyBuilder.LinkedResources.Clear();
            bodyBuilder.Attachments.Add($"Invoice_{invoiceNumber}.pdf", pdfBytes, new ContentType("application", "pdf"));

            message.Body = bodyBuilder.ToMessageBody();
            await SendMessageAsync(message);
        }

        /// <summary>
        /// Public method so the controller can generate a PDF for download without sending email.
        /// </summary>
        public byte[] GenerateInvoicePdfBytes(
            string customerName, string customerEmail, string customerPhone,
            string invoiceNumber,
            string vehicleName, string vehicleDetails, string rentalPeriod,
            int days, decimal ratePerDay, decimal depositAmount, decimal totalAmount,
            string paymentMethod, DateTime paymentDate, int paymentId,
            string? stripeRef = null)
        {
            return GenerateInvoicePdf(customerName, customerEmail, invoiceNumber, vehicleName, vehicleDetails,
                rentalPeriod, days, ratePerDay, depositAmount, totalAmount, paymentMethod, paymentDate, stripeRef,
                customerPhone, paymentId);
        }

        private static byte[] GenerateInvoicePdf(
            string customerName, string customerEmail, string invoiceNumber,
            string vehicleName, string vehicleDetails, string rentalPeriod,
            int days, decimal ratePerDay, decimal depositAmount, decimal totalAmount,
            string paymentMethod, DateTime paymentDate, string? stripeRef,
            string? customerPhone = null, int? paymentId = null)
        {
            // Colors matching the website design
            var primaryBlue = "#0061f2";
            var secondaryTeal = "#00b4d8";
            var darkText = "#1e293b";
            var mutedText = "#64748b";
            var lightBg = "#f0f9ff";
            var borderColor = "#e2e8f0";
            var successGreen = "#10b981";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(mainCol =>
                    {
                        // ═══════════════════════════════════════════════════
                        // GRADIENT HEADER (matching website's var(--gradient))
                        // ═══════════════════════════════════════════════════
                        mainCol.Item().Background(primaryBlue).Padding(30).Row(headerRow =>
                        {
                            headerRow.RelativeItem().Column(left =>
                            {
                                left.Item().Text("RentWheels")
                                    .FontSize(26).Bold().FontColor(Colors.White);
                                left.Item().Text("Invoice for rental booking")
                                    .FontSize(11).FontColor("#ffffffbb");
                            });

                            headerRow.ConstantItem(160).AlignRight().Column(right =>
                            {
                                right.Item().AlignRight().Text("INVOICE")
                                    .FontSize(18).Bold().FontColor(Colors.White);
                                right.Item().AlignRight().PaddingTop(6)
                                    .Background(Colors.White).Padding(4, Unit.Point)
                                    .AlignCenter()
                                    .Text(invoiceNumber)
                                    .FontSize(11).SemiBold().FontColor(darkText);
                            });
                        });

                        // ═══════════════════════════════════════════════════
                        // BODY CONTENT
                        // ═══════════════════════════════════════════════════
                        mainCol.Item().Padding(30).Column(body =>
                        {
                            // ── Customer & Payment Info ──
                            body.Item().Row(infoRow =>
                            {
                                infoRow.RelativeItem().Column(billTo =>
                                {
                                    billTo.Item().Text("Billed To")
                                        .FontSize(10).FontColor(mutedText).SemiBold();
                                    billTo.Item().PaddingTop(4).Text(customerName)
                                        .FontSize(12).Bold().FontColor(darkText);
                                    billTo.Item().Text(customerEmail)
                                        .FontSize(10).FontColor(mutedText);
                                    if (!string.IsNullOrEmpty(customerPhone))
                                    {
                                        billTo.Item().Text(customerPhone)
                                            .FontSize(10).FontColor(mutedText);
                                    }
                                });

                                infoRow.RelativeItem().AlignRight().Column(invDetails =>
                                {
                                    invDetails.Item().AlignRight().Text("Invoice Details")
                                        .FontSize(10).FontColor(mutedText).SemiBold();
                                    invDetails.Item().PaddingTop(4).AlignRight()
                                        .Text($"Date: {paymentDate:MMMM dd, yyyy}")
                                        .FontSize(10).FontColor(darkText);
                                    if (paymentId.HasValue)
                                    {
                                        invDetails.Item().AlignRight()
                                            .Text($"Payment ID: #{paymentId}")
                                            .FontSize(10).FontColor(darkText);
                                    }

                                    invDetails.Item().AlignRight().PaddingTop(4).Row(methodRow =>
                                    {
                                        methodRow.AutoItem().Text("Method: ").FontSize(10).FontColor(darkText);
                                        var methodColor = paymentMethod == "Stripe" ? primaryBlue : successGreen;
                                        var methodIcon = paymentMethod == "Stripe" ? "Stripe" : "Cash";
                                        methodRow.AutoItem()
                                            .Background(methodColor)
                                            .Padding(2, Unit.Point)
                                            .Text($" {methodIcon} ")
                                            .FontSize(9).FontColor(Colors.White);
                                    });
                                });
                            });

                            // ── Divider ──
                            body.Item().PaddingVertical(15)
                                .LineHorizontal(1).LineColor(borderColor);

                            // ── Rental Details Header ──
                            body.Item().PaddingBottom(10)
                                .Text("Rental Details")
                                .FontSize(12).SemiBold().FontColor(darkText);

                            // ── Rental Details Table (matching website layout) ──
                            body.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4); // Description
                                    columns.RelativeColumn(1); // Days
                                    columns.RelativeColumn(1.2f); // Rate/Day
                                    columns.RelativeColumn(1.2f); // Amount
                                });

                                // Table Header
                                table.Header(header =>
                                {
                                    var headerStyle = TextStyle.Default.FontSize(10).SemiBold().FontColor(mutedText);
                                    header.Cell().Background(lightBg).Padding(10)
                                        .Text("Description").Style(headerStyle);
                                    header.Cell().Background(lightBg).Padding(10).AlignCenter()
                                        .Text("Days").Style(headerStyle);
                                    header.Cell().Background(lightBg).Padding(10).AlignRight()
                                        .Text("Rate/Day").Style(headerStyle);
                                    header.Cell().Background(lightBg).Padding(10).AlignRight()
                                        .Text("Amount").Style(headerStyle);
                                });

                                var rentalCost = ratePerDay * days;

                                // Rental Row
                                table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).Column(desc =>
                                {
                                    desc.Item().Text(vehicleName).FontSize(11).SemiBold().FontColor(darkText);
                                    desc.Item().Text(vehicleDetails).FontSize(9).FontColor(mutedText);
                                    desc.Item().Text(rentalPeriod).FontSize(9).FontColor(mutedText);
                                });
                                table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).AlignCenter().AlignMiddle()
                                    .Text(days.ToString()).FontSize(10).FontColor(darkText);
                                table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).AlignRight().AlignMiddle()
                                    .Text($"\u20b9{ratePerDay:N2}").FontSize(10).FontColor(darkText);
                                table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).AlignRight().AlignMiddle()
                                    .Text($"\u20b9{rentalCost:N2}").FontSize(10).Bold().FontColor(darkText);

                                // Security Deposit Row (if applicable)
                                if (depositAmount > 0)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).Column(dep =>
                                    {
                                        dep.Item().Text("Security Deposit").FontSize(11).SemiBold().FontColor(darkText);
                                        dep.Item().Text("Fully refundable upon safe return").FontSize(9).FontColor(successGreen);
                                    });
                                    table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).AlignCenter().AlignMiddle()
                                        .Text("1").FontSize(10).FontColor(darkText);
                                    table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).AlignRight().AlignMiddle()
                                        .Text($"\u20b9{depositAmount:N2}").FontSize(10).FontColor(darkText);
                                    table.Cell().BorderBottom(1).BorderColor(borderColor).Padding(10).AlignRight().AlignMiddle()
                                        .Text($"\u20b9{depositAmount:N2}").FontSize(10).Bold().FontColor(darkText);
                                }
                            });

                            // ── Totals Section (right-aligned, matching website) ──
                            body.Item().PaddingTop(15).AlignRight().Width(220).Column(totals =>
                            {
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal").FontSize(10).FontColor(darkText);
                                    r.ConstantItem(100).AlignRight().Text($"\u20b9{totalAmount:N2}").FontSize(10).FontColor(darkText);
                                });
                                totals.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text("Tax").FontSize(10).FontColor(darkText);
                                    r.ConstantItem(100).AlignRight().Text("\u20b90.00").FontSize(10).FontColor(darkText);
                                });

                                totals.Item().PaddingVertical(8).LineHorizontal(1).LineColor(borderColor);

                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Total Paid").FontSize(13).Bold().FontColor(darkText);
                                    r.ConstantItem(100).AlignRight().Text($"\u20b9{totalAmount:N2}").FontSize(13).Bold().FontColor(successGreen);
                                });
                            });

                            // ── Divider ──
                            body.Item().PaddingTop(20)
                                .LineHorizontal(1).LineColor(borderColor);

                            // ── Status & Stripe Ref (matching website) ──
                            body.Item().PaddingTop(12).Row(statusRow =>
                            {
                                statusRow.RelativeItem().Row(badge =>
                                {
                                    badge.AutoItem()
                                        .Background(successGreen)
                                        .Padding(6, Unit.Point)
                                        .Text("  ✓  Payment Completed  ")
                                        .FontSize(10).FontColor(Colors.White).SemiBold();
                                });

                                if (!string.IsNullOrEmpty(stripeRef))
                                {
                                    statusRow.RelativeItem().AlignRight().AlignMiddle()
                                        .Text($"Stripe Ref: {stripeRef}")
                                        .FontSize(9).FontColor(mutedText);
                                }
                            });
                        });

                        // ═══════════════════════════════════════════════════
                        // FOOTER (matching website card-footer)
                        // ═══════════════════════════════════════════════════
                        mainCol.Item().ExtendVertical().AlignBottom()
                            .Background(lightBg)
                            .PaddingVertical(15)
                            .AlignCenter()
                            .Text("Thank you for choosing RentWheels! For any queries, contact support@rentwheels.com")
                            .FontSize(9).FontColor(mutedText);
                    });
                });
            });

            return document.GeneratePdf();
        }


        private static string GenerateInvoiceHtml(
            string customerName, string customerEmail, string invoiceNumber,
            string vehicleName, string vehicleDetails, string rentalPeriod,
            int days, decimal ratePerDay, decimal depositAmount, decimal totalAmount,
            string paymentMethod, DateTime paymentDate, string? stripeRef)
        {
            var methodBadge = paymentMethod == "Stripe"
                ? "<span style=\"background:#0061f2;color:#fff;padding:4px 12px;border-radius:20px;font-size:13px;\">💳 Stripe</span>"
                : "<span style=\"background:#10b981;color:#fff;padding:4px 12px;border-radius:20px;font-size:13px;\">💵 Cash</span>";

            var stripeSection = !string.IsNullOrEmpty(stripeRef)
                ? $"<tr><td style=\"padding:8px 0;color:#6b7280;font-size:13px;\">Stripe Reference</td><td style=\"padding:8px 0;text-align:right;font-size:13px;color:#374151;\">{stripeRef}</td></tr>"
                : "";

            var rentalCost = ratePerDay * days;

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8"" /><meta name=""viewport"" content=""width=device-width,initial-scale=1"" /></head>
<body style=""margin:0;padding:0;background:#f3f4f6;font-family:'Segoe UI',Roboto,Arial,sans-serif;"">
<div style=""max-width:600px;margin:0 auto;padding:24px;"">

  <!-- Header -->
  <div style=""background:linear-gradient(135deg,#0061f2,#00b4d8);border-radius:16px 16px 0 0;padding:32px;text-align:center;"">
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

    <!-- Rental Info Table -->
    <div style=""border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;margin-bottom:24px;"">
      <table style=""width:100%;border-collapse:collapse;"">
        <thead style=""background:#f9fafb;"">
          <tr>
            <th style=""padding:12px 16px;text-align:left;font-size:12px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;"">Description</th>
            <th style=""padding:12px 16px;text-align:center;font-size:12px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;"">Days</th>
            <th style=""padding:12px 16px;text-align:right;font-size:12px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;"">Rate/Day</th>
            <th style=""padding:12px 16px;text-align:right;font-size:12px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;"">Amount</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td style=""padding:16px;border-bottom:1px solid #e5e7eb;"">
              <strong style=""color:#1f2937;font-size:14px;"">{vehicleName}</strong><br/>
              <span style=""color:#6b7280;font-size:13px;"">{vehicleDetails}</span><br/>
              <span style=""color:#9ca3af;font-size:12px;"">Period: {rentalPeriod}</span>
            </td>
            <td style=""padding:16px;text-align:center;font-size:14px;color:#374151;border-bottom:1px solid #e5e7eb;vertical-align:top;"">{days}</td>
            <td style=""padding:16px;text-align:right;font-size:14px;color:#374151;border-bottom:1px solid #e5e7eb;vertical-align:top;"">₹{ratePerDay:N2}</td>
            <td style=""padding:16px;text-align:right;font-size:14px;color:#374151;border-bottom:1px solid #e5e7eb;vertical-align:top;"">
              <strong>₹{rentalCost:N2}</strong>
            </td>
          </tr>
          {(depositAmount > 0 ? $@"
          <tr>
            <td style=""padding:16px;border-bottom:1px solid #e5e7eb;"">
              <strong style=""color:#1f2937;font-size:14px;"">Security Deposit</strong><br/>
              <span style=""color:#10b981;font-size:12px;"">↩ Fully refundable upon safe return</span>
            </td>
            <td style=""padding:16px;text-align:center;font-size:14px;color:#374151;border-bottom:1px solid #e5e7eb;vertical-align:top;"">1</td>
            <td style=""padding:16px;text-align:right;font-size:14px;color:#374151;border-bottom:1px solid #e5e7eb;vertical-align:top;"">₹{depositAmount:N2}</td>
            <td style=""padding:16px;text-align:right;font-size:14px;color:#374151;border-bottom:1px solid #e5e7eb;vertical-align:top;"">
              <strong>₹{depositAmount:N2}</strong>
            </td>
          </tr>" : "")}
          {stripeSection}
        </tbody>
      </table>

      <!-- Totals -->
      <div style=""background:#f9fafb;padding:16px;"">
        <table style=""width:100%;"">
          <tr>
            <td style=""padding:4px 0;color:#6b7280;font-size:13px;"">Subtotal</td>
            <td style=""padding:4px 0;text-align:right;font-size:13px;color:#374151;"">₹{totalAmount:N2}</td>
          </tr>
          <tr>
            <td style=""padding:4px 0;color:#6b7280;font-size:13px;"">Tax</td>
            <td style=""padding:4px 0;text-align:right;font-size:13px;color:#374151;"">₹0.00</td>
          </tr>
          <tr><td colspan=""2"" style=""padding-top:8px;""><hr style=""border:0;border-top:1px solid #e5e7eb;margin:0;""/></td></tr>
          <tr>
            <td style=""padding-top:12px;font-weight:600;color:#1f2937;font-size:16px;"">Total Paid</td>
            <td style=""padding-top:12px;text-align:right;font-weight:700;color:#10b981;font-size:18px;"">₹{totalAmount:N2}</td>
          </tr>
        </table>
      </div>
    </div>

    <!-- Refund Info -->
    {(depositAmount > 0 ? $@"
    <div style=""background:#fcfdfd;border:1px dashed #d1d5db;border-radius:12px;padding:16px;display:flex;align-items:flex-start;gap:12px;"">
      <div style=""font-size:24px;"">💡</div>
      <div>
        <h4 style=""margin:0 0 4px;font-size:14px;color:#374151;"">Deposit Refund</h4>
        <p style=""margin:0;font-size:13px;color:#6b7280;line-height:1.4;"">
          Your security deposit of <strong>₹{depositAmount:N2}</strong> will be refunded automatically upon safe return of the vehicle.
        </p>
      </div>
    </div>" : "")}

    <!-- PDF Attachment Note -->
    <div style=""background:#f0f9ff;border:1px solid #bae6fd;border-radius:12px;padding:16px;margin-top:16px;text-align:center;"">
      <p style=""margin:0;font-size:13px;color:#0369a1;"">📎 Your invoice PDF is attached to this email for your records.</p>
    </div>

  </div>

  <!-- Footer -->
  <p style=""text-align:center;font-size:12px;color:#9ca3af;margin-top:24px;"">
    Thank you for choosing RentWheels!<br/>
    If you have any questions, reply to this email or contact support.
  </p>

</div>
</body>
</html>
";
        }

        public async Task SendDepositRefundEmailAsync(
            string toEmail, string customerName, string vehicleName, int bookingId,
            decimal depositAmount, decimal damageCost, decimal refundAmount,
            string? stripeRefundId, string? damagePdfLink)
        {
            var senderEmail = (_configuration["Email:SenderEmail"] ?? string.Empty).Trim();
            var senderName = _configuration["Email:SenderName"] ?? "RentWheels";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(customerName, toEmail));
            message.Subject = $"RentWheels — Deposit Refund Processed (Booking #{bookingId})";

            var htmlBody = GenerateDepositRefundHtml(customerName, vehicleName, bookingId, depositAmount, damageCost, refundAmount, stripeRefundId, damagePdfLink);

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();
            await SendMessageAsync(message);
        }

        private static string GenerateDepositRefundHtml(
            string customerName, string vehicleName, int bookingId,
            decimal depositAmount, decimal damageCost, decimal refundAmount,
            string? stripeRefundId, string? damagePdfLink)
        {
            var stripeInfo = !string.IsNullOrEmpty(stripeRefundId) 
                ? $"<p><strong>Stripe Refund ID:</strong> {stripeRefundId}</p>" : "";

            var damageInfo = damageCost > 0 
                ? $"<p><strong>Damage Cost Deducted:</strong> ₹{damageCost:N2}</p>" : "";
                
            var damageLinkHtml = !string.IsNullOrEmpty(damagePdfLink) 
                ? $"<p><a href=\"{damagePdfLink}\" style=\"display:inline-block;padding:10px 20px;background:#0061f2;color:#fff;border-radius:6px;text-decoration:none;margin-top:10px;\">View Damage Report</a></p>" 
                : "";

            return $@"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; background: #f3f4f6; margin: 0; padding: 20px;"">
    <div style=""max-width: 600px; margin: auto; background: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
        <h2 style=""color: #0061f2;"">Deposit Refund Processed</h2>
        <p>Hi {customerName},</p>
        <p>Your deposit refund for the rental of <strong>{vehicleName}</strong> (Booking #{bookingId}) has been processed.</p>
        
        <div style=""background: #f9fafb; padding: 15px; border-radius: 6px; margin: 20px 0;"">
            <p><strong>Original Deposit:</strong> ₹{depositAmount:N2}</p>
            {damageInfo}
            <p><strong>Total Refunded:</strong> ₹{refundAmount:N2}</p>
            {stripeInfo}
        </div>

        {damageLinkHtml}

        <p style=""color: #6b7280; font-size: 14px; margin-top: 30px;"">
            Please allow 5-10 business days for the funds to appear in your account depending on your bank.
        </p>
        <p style=""color: #6b7280; font-size: 14px;"">Thank you for choosing RentWheels!</p>
    </div>
</body>
</html>";
        }
    }
}
