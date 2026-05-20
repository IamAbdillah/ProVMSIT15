using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;
using ProVMSIT15.Models;
using ProVMSIT15.Services;

namespace ProVMSIT15.Controllers;

[Authorize(Policy = "AdminOnly")]
public class UserManagementController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationService _notif;

    public UserManagementController(ApplicationDbContext db, NotificationService notif)
    {
        _db = db;
        _notif = notif;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "User Management";
        ViewData["BreadcrumbModule"] = "User Management";
        var users = await _db.Users
            .OrderBy(u => u.UserRole).ThenBy(u => u.FullName)
            .ToListAsync();
        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Provision New User";
        ViewData["BreadcrumbModule"] = "User Management";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string fullName, string email, string password,
        UserRole role, string? departmentCode)
    {
        ViewData["Title"] = "Provision New User";
        ViewData["BreadcrumbModule"] = "User Management";

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "All fields are required.";
            return View();
        }

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            TempData["Error"] = "Email already registered.";
            return View();
        }

        if (role == UserRole.Vendor)
        {
            TempData["Error"] = "Vendor accounts must self-register through the public portal.";
            return View();
        }

        var user = new AppUser
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            UserRole = role,
            DepartmentCode = departmentCode?.Trim().ToUpper(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"User '{fullName}' provisioned as {role}. They may now log in.";
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(int id, UserRole role)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (role == UserRole.Vendor)
        {
            TempData["Error"] = "Cannot assign Vendor role to internal users.";
            return RedirectToAction("Index");
        }

        var currentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (user.ID == currentId)
        {
            TempData["Error"] = "You cannot change your own role.";
            return RedirectToAction("Index");
        }

        user.UserRole = role;
        await _db.SaveChangesAsync();
        await _notif.SendAsync(user.ID, $"Your system role has been updated to {role} by an Administrator.");

        TempData["Success"] = $"{user.FullName}'s role updated to {role}.";
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var currentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == currentId)
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction("Index");
        }

        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"User '{user.FullName}' removed from the system.";
        return RedirectToAction("Index");
    }
}
