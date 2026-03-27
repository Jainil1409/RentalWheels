using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vehicle_management_system_mvc.Data;
using vehicle_management_system_mvc.Models;
using vehicle_management_system_mvc.ViewModels;

namespace vehicle_management_system_mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = model.Password,
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await SignInUser(user);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || user.PasswordHash != model.Password)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            await SignInUser(user);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Profile(string? returnUrl = null)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var model = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DriverLicenseNumber = user.DriverLicenseNumber ?? "",
                LicenseExpiryDate = user.LicenseExpiryDate,
                Address = user.Address ?? "",
                ExistingIdProofUrl = user.IdProofUrl,
                IsVerified = user.IsVerified
            };

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileViewModel model, string? returnUrl = null)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.DriverLicenseNumber = model.DriverLicenseNumber;
                
                // Convert DateTime to UTC for PostgreSQL
                if (model.LicenseExpiryDate.HasValue)
                {
                    user.LicenseExpiryDate = DateTime.SpecifyKind(model.LicenseExpiryDate.Value, DateTimeKind.Utc);
                }
                else
                {
                    user.LicenseExpiryDate = null;
                }

                user.Address = model.Address;

                if (model.IdProofImage != null && model.IdProofImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "idproofs");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.IdProofImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.IdProofImage.CopyToAsync(fileStream);
                    }
                    user.IdProofUrl = "/images/idproofs/" + uniqueFileName;
                    user.IsVerified = false; // reset verification if new ID uploaded
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Profile");
            }
            
            model.ExistingIdProofUrl = user.IdProofUrl;
            model.IsVerified = user.IsVerified;
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllUsers()
        {
            var users = await _context.Users.Where(u => u.Role == UserRole.Customer).OrderByDescending(u => u.CreatedAt).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsVerified = true;

                // Notify user
                var notification = new Notification
                {
                    UserId = user.Id,
                    Message = "Your account has been verified by the admin. You can now book vehicles!",
                    Type = "AccountVerification",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();
                TempData["Success"] = $"{user.FullName} has been verified successfully.";
            }
            return RedirectToAction(nameof(AllUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View("Login");
            }

            var info = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!info.Succeeded)
            {
                // In ASP.NET Core MVC with cookies, typically Challenge is redirected back.
                // But wait, the default challenge scheme for Google returns to the default SignInScheme.
                // Let's read claims from the external identity.
                // Usually we use a separate scheme for External Cookie, but if we just use the default cookie, we check if they are logged in.
                
                // Let's use the standard approach: re-authenticate the temporary identity
                var result = await HttpContext.AuthenticateAsync();
                if (!result.Succeeded)
                {
                    return RedirectToAction("Login");
                }
                
                info = result;
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email claim not received from provider.");
                return View("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                // User exists, sign them in with our cookie scheme logic
                await HttpContext.SignOutAsync(); // Sign out of the temporary external cookie if any
                await SignInUser(user);
                return LocalRedirect(returnUrl ?? "/");
            }

            // User does not exist, ask for phone number to complete registration
            await HttpContext.SignOutAsync(); // Don't persist partial login

            var model = new ExternalLoginConfirmationViewModel
            {
                Email = email,
                FullName = name ?? ""
            };

            ViewData["ReturnUrl"] = returnUrl;
            return View("ExternalLoginConfirmation", model);
        }

        [HttpGet]
        public IActionResult ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmationPost(ExternalLoginConfirmationViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View("ExternalLoginConfirmation", model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    PasswordHash = "OAUTH_GOOGLE", // Dummy password since they use Google
                    Role = UserRole.Customer,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await SignInUser(user);
                return LocalRedirect(returnUrl ?? "/");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View("ExternalLoginConfirmation", model);
        }

        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };
            
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(TimeSpan.FromHours(8).TotalSeconds)
                }
            );
        }
    }
}
