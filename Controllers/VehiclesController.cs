using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Models;
using vehicle_management_system_mvc.Services;
using vehicle_management_system_mvc.ViewModels;

namespace vehicle_management_system_mvc.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UnsplashService _unsplashService;

        public VehiclesController(ApplicationDbContext context, UnsplashService unsplashService)
        {
            _context = context;
            _unsplashService = unsplashService;
        }

        public async Task<IActionResult> Index(VehicleSearchViewModel search)
        {
            var query = _context.Vehicles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search.Brand))
                query = query.Where(v => v.Brand.Contains(search.Brand));

            if (search.Type.HasValue)
                query = query.Where(v => v.Type == search.Type.Value);

            if (search.MinPrice.HasValue)
                query = query.Where(v => v.PricePerDay >= search.MinPrice.Value);

            if (search.MaxPrice.HasValue)
                query = query.Where(v => v.PricePerDay <= search.MaxPrice.Value);

            if (search.AvailableOnly == true)
                query = query.Where(v => v.IsAvailable);

            search.Vehicles = await query.OrderBy(v => v.Brand).ThenBy(v => v.Model).ToListAsync();

            ViewBag.VehicleTypes = new SelectList(Enum.GetValues<VehicleType>());
            return View(search);
        }

        public async Task<IActionResult> Details(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();
            return View(vehicle);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
            if (!ModelState.IsValid)
                return View(vehicle);

            if (string.IsNullOrEmpty(vehicle.ImageUrl))
            {
                vehicle.ImageUrl = await _unsplashService.GetVehicleImageUrlAsync(vehicle.Brand, vehicle.Model);
            }

            vehicle.CreatedAt = DateTime.UtcNow;
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Vehicle added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();
            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(vehicle);

            var existing = await _context.Vehicles.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Brand = vehicle.Brand;
            existing.Model = vehicle.Model;
            existing.Type = vehicle.Type;
            existing.PricePerDay = vehicle.PricePerDay;
            existing.IsAvailable = vehicle.IsAvailable;
            existing.Description = vehicle.Description;
            existing.Year = vehicle.Year;
            existing.LicensePlate = vehicle.LicensePlate;

            if (string.IsNullOrEmpty(vehicle.ImageUrl))
            {
                existing.ImageUrl = await _unsplashService.GetVehicleImageUrlAsync(vehicle.Brand, vehicle.Model);
            }
            else
            {
                existing.ImageUrl = vehicle.ImageUrl;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Vehicle updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();
            return View(vehicle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Bookings)
                    .ThenInclude(b => b.Payment)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle == null) return NotFound();

            // Remove payments tied to this vehicle's bookings
            var payments = vehicle.Bookings
                .Where(b => b.Payment != null)
                .Select(b => b.Payment!);
            _context.Payments.RemoveRange(payments);

            // Remove bookings
            _context.Bookings.RemoveRange(vehicle.Bookings);

            // Remove vehicle
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Vehicle deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            vehicle.IsAvailable = !vehicle.IsAvailable;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Vehicle marked as {(vehicle.IsAvailable ? "Available" : "Unavailable")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefreshImages()
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.ImageUrl == null || v.ImageUrl == "" || v.ImageUrl.StartsWith("/images/"))
                .ToListAsync();

            int updated = 0;
            foreach (var vehicle in vehicles)
            {
                var url = await _unsplashService.GetVehicleImageUrlAsync(vehicle.Brand, vehicle.Model);
                if (!string.IsNullOrEmpty(url))
                {
                    vehicle.ImageUrl = url;
                    updated++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Refreshed images for {updated} vehicle(s).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateRandomCars(int count = 5)
        {
            var carData = new (string Brand, string Model, VehicleType Type, decimal MinPrice, decimal MaxPrice, int MinYear, int MaxYear, string Description)[]
            {
                ("Toyota", "Camry", VehicleType.Sedan, 12000, 18000, 2021, 2025, "Reliable mid-size sedan with excellent fuel economy and modern safety features."),
                ("Toyota", "Corolla", VehicleType.Sedan, 10000, 15000, 2021, 2025, "Compact sedan known for its dependability and low running costs."),
                ("Honda", "Civic", VehicleType.Sedan, 11000, 16000, 2022, 2025, "Sporty compact sedan with a refined interior and turbocharged engine."),
                ("Honda", "CR-V", VehicleType.SUV, 15000, 22000, 2022, 2025, "Spacious SUV perfect for family trips with Honda Sensing safety suite."),
                ("Ford", "Mustang", VehicleType.Convertible, 22000, 30000, 2022, 2025, "Iconic American muscle car with thrilling performance and bold styling."),
                ("Ford", "F-150", VehicleType.Truck, 18000, 25000, 2021, 2025, "Best-selling pickup truck with powerful towing and rugged capability."),
                ("BMW", "3 Series", VehicleType.Luxury, 20000, 28000, 2022, 2025, "Premium luxury sedan with dynamic handling and cutting-edge technology."),
                ("BMW", "X5", VehicleType.SUV, 22000, 30000, 2022, 2025, "Luxury SUV combining performance, comfort, and advanced all-wheel drive."),
                ("Mercedes", "C-Class", VehicleType.Luxury, 20000, 28000, 2022, 2025, "Elegant luxury sedan with a refined cabin and smooth ride quality."),
                ("Mercedes", "GLE", VehicleType.SUV, 23000, 30000, 2022, 2025, "Premium mid-size SUV with spacious interior and powerful engine options."),
                ("Audi", "A4", VehicleType.Luxury, 19000, 26000, 2022, 2025, "Sophisticated sedan with quattro AWD and virtual cockpit technology."),
                ("Audi", "Q7", VehicleType.SUV, 22000, 29000, 2022, 2025, "Three-row luxury SUV with a refined ride and high-tech cabin."),
                ("Tesla", "Model 3", VehicleType.Sedan, 18000, 25000, 2023, 2025, "All-electric sedan with autopilot, fast charging, and zero emissions."),
                ("Tesla", "Model Y", VehicleType.SUV, 20000, 27000, 2023, 2025, "Electric compact SUV with impressive range and minimalist design."),
                ("Chevrolet", "Tahoe", VehicleType.SUV, 18000, 25000, 2021, 2025, "Full-size SUV with seating for up to 8 and powerful V8 engine."),
                ("Chevrolet", "Camaro", VehicleType.Convertible, 20000, 28000, 2021, 2024, "Classic American sports car with aggressive styling and V8 power."),
                ("Jeep", "Wrangler", VehicleType.SUV, 16000, 22000, 2021, 2025, "Legendary off-road SUV with removable top and rugged 4x4 capability."),
                ("Volkswagen", "Golf", VehicleType.Hatchback, 10000, 15000, 2021, 2025, "Versatile hatchback with refined handling and turbocharged efficiency."),
                ("Hyundai", "Tucson", VehicleType.SUV, 14000, 20000, 2022, 2025, "Modern SUV with bold design, hybrid option, and advanced safety tech."),
                ("Hyundai", "Elantra", VehicleType.Sedan, 10000, 14000, 2022, 2025, "Stylish compact sedan with a striking design and great fuel economy."),
                ("Kia", "Sportage", VehicleType.SUV, 14000, 20000, 2022, 2025, "Compact SUV with a spacious cabin and cutting-edge infotainment."),
                ("Porsche", "911", VehicleType.Convertible, 25000, 30000, 2022, 2025, "Legendary sports car with rear-engine layout and exhilarating performance."),
                ("Range Rover", "Sport", VehicleType.Luxury, 25000, 30000, 2022, 2025, "Ultimate luxury SUV with commanding presence and off-road prowess."),
                ("Toyota", "Tacoma", VehicleType.Truck, 15000, 20000, 2021, 2025, "Mid-size pickup truck built for adventure with trail-ready capability."),
                ("Nissan", "Altima", VehicleType.Sedan, 11000, 16000, 2021, 2025, "Comfortable mid-size sedan with Nissan Safety Shield and AWD option."),
                ("Mazda", "CX-5", VehicleType.SUV, 14000, 20000, 2022, 2025, "Premium-feeling compact SUV with engaging driving dynamics."),
                ("Subaru", "Outback", VehicleType.SUV, 14000, 20000, 2022, 2025, "All-wheel drive crossover wagon perfect for outdoor adventures."),
                ("Dodge", "Charger", VehicleType.Sedan, 18000, 25000, 2021, 2025, "Bold performance sedan with available HEMI V8 and aggressive stance."),
                ("Ford", "Explorer", VehicleType.SUV, 16000, 22000, 2022, 2025, "Three-row family SUV with rear-wheel drive platform and strong towing."),
                ("Ram", "1500", VehicleType.Truck, 17000, 24000, 2021, 2025, "Full-size pickup with a luxurious interior and smooth coil-spring rear suspension.")
            };

            var random = new Random();
            var selectedIndices = Enumerable.Range(0, carData.Length)
                .OrderBy(_ => random.Next())
                .Take(Math.Min(count, carData.Length))
                .ToList();

            int generated = 0;
            foreach (var idx in selectedIndices)
            {
                var car = carData[idx];
                var price = Math.Round((decimal)(random.NextDouble() * (double)(car.MaxPrice - car.MinPrice)) + car.MinPrice, 2);
                var year = random.Next(car.MinYear, car.MaxYear + 1);
                var plate = $"{(char)('A' + random.Next(26))}{(char)('A' + random.Next(26))}{(char)('A' + random.Next(26))}-{random.Next(1000, 9999)}";

                var imageUrl = await _unsplashService.GetVehicleImageUrlAsync(car.Brand, car.Model);

                var vehicle = new Vehicle
                {
                    Brand = car.Brand,
                    Model = car.Model,
                    Type = car.Type,
                    PricePerDay = price,
                    IsAvailable = random.Next(100) < 80,
                    Description = car.Description,
                    ImageUrl = imageUrl,
                    Year = year,
                    LicensePlate = plate,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Vehicles.Add(vehicle);
                generated++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Generated {generated} random vehicle(s) with Unsplash images.";
            return RedirectToAction(nameof(Index));
        }
    }
}
