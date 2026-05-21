using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;
using ProVMSIT15.Models;
using ProVMSIT15.Models.ViewModels;
using ProVMSIT15.Services;

namespace ProVMSIT15.Controllers;

public class AuthController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;
    private readonly RecaptchaService _recaptcha;
    private readonly string _recaptchaSiteKey;

    public AuthController(ApplicationDbContext db, JwtService jwt, RecaptchaService recaptcha, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _recaptcha = recaptcha;
        _recaptchaSiteKey = config["ReCaptcha:SiteKey"]!;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        ViewBag.RecaptchaSiteKey = _recaptchaSiteKey;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewBag.RecaptchaSiteKey = _recaptchaSiteKey;
        if (!ModelState.IsValid) return View(model);

        // reCAPTCHA verification
        var captchaToken = Request.Form["g-recaptcha-response"].ToString();
        if (!await _recaptcha.VerifyAsync(captchaToken))
        {
            ModelState.AddModelError("", "Please complete the reCAPTCHA verification.");
            return View(model);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        // Phase A: Pre-authentication lockout gate
        if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            ModelState.AddModelError("", "This account has been temporarily locked due to multiple invalid login attempts. Please try again in 15 minutes.");
            return View(model);
        }

        // Phase B: Failed credential check
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            if (user != null)
            {
                user.AccessFailedCount++;
                if (user.AccessFailedCount >= 5)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                    user.AccessFailedCount = 0;
                    await _db.SaveChangesAsync();
                    ModelState.AddModelError("", "This account has been temporarily locked due to multiple invalid login attempts. Please try again in 15 minutes.");
                    return View(model);
                }
                await _db.SaveChangesAsync();
            }
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // Phase C: Successful login — reset lockout state
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await _db.SaveChangesAsync();

        // Vendor accreditation gate — block login until approved
        if (user.UserRole == UserRole.Vendor)
        {
            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.LinkedUserID == user.ID);
            if (vendor == null || vendor.OperationalStatus == OperationalStatus.PendingVerification)
            {
                ModelState.AddModelError("", "Your vendor application is still pending accreditation review. You will be notified once approved.");
                return View(model);
            }
            if (vendor.OperationalStatus == OperationalStatus.Suspended || vendor.OperationalStatus == OperationalStatus.Blacklisted)
            {
                ModelState.AddModelError("", "Your vendor account has been suspended. Please contact procurement for assistance.");
                return View(model);
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.UserRole.ToString()),
            new("DepartmentCode", user.DepartmentCode ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        var token = _jwt.GenerateToken(user);
        Response.Cookies.Append("provms_jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(480)
        });

        return user.UserRole switch
        {
            UserRole.Admin => RedirectToAction("Index", "Dashboard"),
            UserRole.Procurement => RedirectToAction("Index", "Dashboard"),
            UserRole.Finance => RedirectToAction("Index", "Dashboard"),
            UserRole.Vendor => RedirectToAction("Profile", "Vendor"),
            _ => RedirectToAction("Marketplace", "Catalog")
        };
    }

    [HttpGet]
    public IActionResult Register()
    {
        return RedirectToAction("Onboarding", "Vendor");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        return StatusCode(403, "HTTP 403 Forbidden: Public registration is exclusively for vendor accounts. " +
            "Internal staff accounts must be provisioned by a System Administrator via the User Management module.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("provms_jwt");
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
