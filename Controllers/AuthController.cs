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

    public AuthController(ApplicationDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
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
