using vehicle_management_system_mvc.Models;

namespace vehicle_management_system_mvc.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (!context.Users.Any(u => u.Email == "admin123@gmail.com"))
            {
                context.Users.Add(new User
                {
                    FullName = "System Admin",
                    Email = "admin123@gmail.com",
                    Phone = "1234567890",
                    PasswordHash = "admin123",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            else
            {
                var adminUser = context.Users.First(u => u.Email == "admin123@gmail.com");
                adminUser.PasswordHash = "admin123";
                context.Users.Update(adminUser);
                await context.SaveChangesAsync();
            }

            if (!context.Users.Any(u => u.Role == UserRole.Customer))
            {
                context.Users.AddRange(
                    new User { FullName = "Rahul Sharma", Email = "rahul@example.com", Phone = "9876543210", PasswordHash = "Rahul@123", Role = UserRole.Customer, CreatedAt = DateTime.UtcNow },
                    new User { FullName = "Priya Patel", Email = "priya@example.com", Phone = "9876543211", PasswordHash = "Priya@123", Role = UserRole.Customer, CreatedAt = DateTime.UtcNow },
                    new User { FullName = "Amit Kumar", Email = "amit@example.com", Phone = "9876543212", PasswordHash = "Amit@123", Role = UserRole.Customer, CreatedAt = DateTime.UtcNow },
                    new User { FullName = "Sneha Desai", Email = "sneha@example.com", Phone = "9876543213", PasswordHash = "Sneha@123", Role = UserRole.Customer, CreatedAt = DateTime.UtcNow },
                    new User { FullName = "Vikram Singh", Email = "vikram@example.com", Phone = "9876543214", PasswordHash = "Vikram@123", Role = UserRole.Customer, CreatedAt = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();
            }

            if (!context.Vehicles.Any())
            {
                context.Vehicles.AddRange(
                    new Vehicle { Brand = "Toyota", Model = "Camry", Type = VehicleType.Sedan, PricePerDay = 12000.00m, Year = 2023, LicensePlate = "ABC-1234", IsAvailable = true, Description = "Comfortable mid-size sedan with excellent fuel economy. Features include adaptive cruise control, lane departure warning, and a spacious interior.", ImageUrl = "/vehicles/toyota-camry.jpg" },
                    new Vehicle { Brand = "Honda", Model = "CR-V", Type = VehicleType.SUV, PricePerDay = 18000.00m, Year = 2024, LicensePlate = "DEF-5678", IsAvailable = true, Description = "Spacious SUV perfect for family trips. Equipped with Honda Sensing suite, panoramic sunroof, and ample cargo space.", ImageUrl = "/vehicles/honda-crv.jpg" },
                    new Vehicle { Brand = "Ford", Model = "F-150", Type = VehicleType.Truck, PricePerDay = 20000.00m, Year = 2023, LicensePlate = "GHI-9012", IsAvailable = true, Description = "Powerful pickup truck for heavy-duty tasks. Features a 3.5L EcoBoost engine, towing capacity of 13,000 lbs, and a rugged design.", ImageUrl = "/vehicles/ford-f150.jpg" },
                    new Vehicle { Brand = "BMW", Model = "5 Series", Type = VehicleType.Luxury, PricePerDay = 25000.00m, Year = 2024, LicensePlate = "JKL-3456", IsAvailable = false, Description = "Premium luxury sedan with advanced features. Includes leather interior, heads-up display, and a powerful inline-6 engine.", ImageUrl = "/vehicles/bmw-5series.jpg" },
                    new Vehicle { Brand = "Volkswagen", Model = "Golf", Type = VehicleType.Hatchback, PricePerDay = 10000.00m, Year = 2023, LicensePlate = "MNO-7890", IsAvailable = true, Description = "Compact and efficient hatchback for city driving. Known for its refined handling, turbocharged engine, and sleek design.", ImageUrl = "/vehicles/vw-golf.jpg" },
                    new Vehicle { Brand = "Mercedes", Model = "Sprinter", Type = VehicleType.Van, PricePerDay = 22000.00m, Year = 2022, LicensePlate = "PQR-1234", IsAvailable = true, Description = "Large cargo van ideal for moving and deliveries. Offers a high roof, up to 488 cu ft of cargo space, and a diesel engine for efficiency.", ImageUrl = "/vehicles/mercedes-sprinter.jpg" },
                    new Vehicle { Brand = "Hyundai", Model = "Tucson", Type = VehicleType.SUV, PricePerDay = 15000.00m, Year = 2024, LicensePlate = "STU-5678", IsAvailable = true, Description = "Modern SUV with great mileage and comfort. Features a bold parametric design, Hyundai SmartSense, and a hybrid option.", ImageUrl = "/vehicles/hyundai-tucson.jpg" },
                    new Vehicle { Brand = "Audi", Model = "A4", Type = VehicleType.Luxury, PricePerDay = 24000.00m, Year = 2024, LicensePlate = "VWX-9012", IsAvailable = true, Description = "Stylish luxury sedan with cutting-edge technology. Includes Audi virtual cockpit, quattro all-wheel drive, and sport suspension.", ImageUrl = "/vehicles/audi-a4.jpg" }
                );
                await context.SaveChangesAsync();
            }

            if (!context.Bookings.Any())
            {
                var rahul = context.Users.FirstOrDefault(u => u.Email == "rahul@example.com");
                var priya = context.Users.FirstOrDefault(u => u.Email == "priya@example.com");
                var amit = context.Users.FirstOrDefault(u => u.Email == "amit@example.com");
                var sneha = context.Users.FirstOrDefault(u => u.Email == "sneha@example.com");
                var vikram = context.Users.FirstOrDefault(u => u.Email == "vikram@example.com");

                var camry = context.Vehicles.FirstOrDefault(v => v.LicensePlate == "ABC-1234");
                var crv = context.Vehicles.FirstOrDefault(v => v.LicensePlate == "DEF-5678");
                var f150 = context.Vehicles.FirstOrDefault(v => v.LicensePlate == "GHI-9012");
                var bmw = context.Vehicles.FirstOrDefault(v => v.LicensePlate == "JKL-3456");
                var golf = context.Vehicles.FirstOrDefault(v => v.LicensePlate == "MNO-7890");
                var sprinter = context.Vehicles.FirstOrDefault(v => v.LicensePlate == "PQR-1234");

                if (rahul != null && priya != null && amit != null && sneha != null && vikram != null
                    && camry != null && crv != null && f150 != null && bmw != null && golf != null && sprinter != null)
                {

                var bookings = new List<Booking>
                {
                    // Completed booking with payment
                    new Booking
                    {
                        CustomerId = rahul.Id, VehicleId = camry.Id,
                        StartDate = DateTime.UtcNow.AddDays(-20), EndDate = DateTime.UtcNow.AddDays(-15),
                        TotalCost = 5 * camry.PricePerDay, Status = BookingStatus.Completed, CreatedAt = DateTime.UtcNow.AddDays(-22)
                    },
                    // Active rental (approved) — BMW is marked unavailable for this
                    new Booking
                    {
                        CustomerId = priya.Id, VehicleId = bmw.Id,
                        StartDate = DateTime.UtcNow.AddDays(-3), EndDate = DateTime.UtcNow.AddDays(4),
                        TotalCost = 7 * bmw.PricePerDay, Status = BookingStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    // Completed booking with payment
                    new Booking
                    {
                        CustomerId = amit.Id, VehicleId = f150.Id,
                        StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(-27),
                        TotalCost = 3 * f150.PricePerDay, Status = BookingStatus.Completed, CreatedAt = DateTime.UtcNow.AddDays(-32)
                    },
                    // Pending booking
                    new Booking
                    {
                        CustomerId = sneha.Id, VehicleId = golf.Id,
                        StartDate = DateTime.UtcNow.AddDays(2), EndDate = DateTime.UtcNow.AddDays(6),
                        TotalCost = 4 * golf.PricePerDay, Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    // Approved booking awaiting payment
                    new Booking
                    {
                        CustomerId = vikram.Id, VehicleId = sprinter.Id,
                        StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(5),
                        TotalCost = 4 * sprinter.PricePerDay, Status = BookingStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    // Rejected booking
                    new Booking
                    {
                        CustomerId = rahul.Id, VehicleId = crv.Id,
                        StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-7),
                        TotalCost = 3 * crv.PricePerDay, Status = BookingStatus.Rejected, CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    // Cancelled booking
                    new Booking
                    {
                        CustomerId = priya.Id, VehicleId = golf.Id,
                        StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow.AddDays(-2),
                        TotalCost = 3 * golf.PricePerDay, Status = BookingStatus.Cancelled, CreatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    // Overdue rental (approved but end date passed)
                    new Booking
                    {
                        CustomerId = amit.Id, VehicleId = crv.Id,
                        StartDate = DateTime.UtcNow.AddDays(-10), EndDate = DateTime.UtcNow.AddDays(-2),
                        TotalCost = 8 * crv.PricePerDay, Status = BookingStatus.Approved, CreatedAt = DateTime.UtcNow.AddDays(-12)
                    }
                };

                context.Bookings.AddRange(bookings);
                await context.SaveChangesAsync();
                }
            }

            if (!context.Payments.Any())
            {
                var completedBookings = context.Bookings
                    .Where(b => b.Status == BookingStatus.Completed)
                    .ToList();

                foreach (var booking in completedBookings)
                {
                    context.Payments.Add(new Payment
                    {
                        BookingId = booking.Id,
                        Amount = booking.TotalCost,
                        PaymentDate = booking.EndDate,
                        Method = PaymentMethod.Cash,
                        Status = PaymentStatus.Completed
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
