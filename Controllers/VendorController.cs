using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;
using ProVMSIT15.Models;
using ProVMSIT15.Models.ViewModels;
using ProVMSIT15.Services;

namespace ProVMSIT15.Controllers;

public class VendorController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationService _notif;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public VendorController(ApplicationDbContext db, NotificationService notif,
        IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _notif = notif;
        _config = config;
        _env = env;
    }

    [HttpGet, Authorize(Policy = "ProcurementOrAdmin")]
    public async Task<IActionResult> EvaluationDesk()
    {
        ViewData["Title"] = "Vendor Evaluation";
        ViewData["BreadcrumbModule"] = "Vendor Accreditation";
        var evals = await _db.SupplierEvaluations
            .Include(e => e.Vendor)
            .Include(e => e.Requisition).ThenInclude(r => r!.Item)
            .OrderByDescending(e => e.CreatedDate)
            .ToListAsync();
        return View(evals);
    }

    [HttpGet]
    public IActionResult Onboarding() => View(new VendorOnboardingViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onboarding(VendorOnboardingViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _db.Vendors.AnyAsync(v => v.TaxID == model.TaxID))
        {
            ModelState.AddModelError("TaxID", "A vendor with this Tax ID already exists.");
            return View(model);
        }

        if (await _db.Users.AnyAsync(u => u.Email == model.ContactEmail))
        {
            ModelState.AddModelError("ContactEmail", "Email already registered.");
            return View(model);
        }

        string? docUrl = null;
        if (model.DocumentFile != null && model.DocumentFile.Length > 0)
        {
            var maxSize = _config.GetValue<long>("FileUpload:MaxFileSizeBytes", 5242880);
            var allowed = _config["FileUpload:AllowedExtensions"] ?? ".pdf";
            var ext = Path.GetExtension(model.DocumentFile.FileName).ToLowerInvariant();

            if (ext != allowed)
            {
                ModelState.AddModelError("DocumentFile", "Only PDF files are accepted.");
                return View(model);
            }
            if (model.DocumentFile.Length > maxSize)
            {
                ModelState.AddModelError("DocumentFile", "File exceeds 5MB limit.");
                return View(model);
            }

            var uploadPath = Path.Combine(_env.ContentRootPath, _config["FileUpload:UploadPath"] ?? "wwwroot/uploads/documents");
            System.IO.Directory.CreateDirectory(uploadPath);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadPath, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.DocumentFile.CopyToAsync(stream);
            docUrl = $"/uploads/documents/{fileName}";
        }

        var userAccount = new AppUser
        {
            FullName     = model.CompanyName,
            Email        = model.ContactEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            UserRole     = UserRole.Vendor,
            CreatedAt    = DateTime.UtcNow
        };
        _db.Users.Add(userAccount);
        await _db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var vendor = new Vendor
        {
            CompanyName       = model.CompanyName,
            TaxID             = model.TaxID,
            ContactEmail      = model.ContactEmail,
            DocumentVaultURL  = docUrl,
            OperationalStatus = OperationalStatus.PendingVerification,
            LinkedUserID      = userAccount.ID,
            SubmittedAt       = now,
            UpdatedAt         = now
        };
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();

        if (model.CatalogItems?.Count > 0)
        {
            foreach (var item in model.CatalogItems.Where(i => !string.IsNullOrWhiteSpace(i.ItemName)))
            {
                _db.VendorItems.Add(new VendorItem
                {
                    VendorID = vendor.ID,
                    ItemName = item.ItemName,
                    Category = item.Category,
                    UnitPrice = item.UnitPrice
                });
            }
            await _db.SaveChangesAsync();
        }

        await _notif.SendToRoleAsync(UserRole.Admin, $"New vendor '{vendor.CompanyName}' submitted for accreditation.");
        await _notif.SendToRoleAsync(UserRole.Procurement, $"New vendor '{vendor.CompanyName}' pending review.");

        TempData["Success"] = "Registration submitted. Await accreditation approval.";
        return RedirectToAction("Login", "Auth");
    }

    [HttpGet]
    [Authorize(Policy = "ProcurementOrAdmin")]
    public async Task<IActionResult> AccreditationDesk()
    {
        ViewData["Title"] = "Accreditation Process";
        ViewData["BreadcrumbModule"] = "Vendor Accreditation";
        var vendors = await _db.Vendors
            .Include(v => v.Items)
            .Include(v => v.LinkedUser)
            .Where(v => v.OperationalStatus == OperationalStatus.PendingVerification)
            .OrderByDescending(v => v.UpdatedAt)
            .ToListAsync();
        return View(vendors);
    }

    [HttpPost]
    [Authorize(Policy = "ProcurementOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveVendor(int id)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor == null) return NotFound();

        vendor.OperationalStatus = OperationalStatus.Active;
        vendor.UpdatedAt = DateTime.UtcNow;
        vendor.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (vendor.LinkedUserID.HasValue)
            await _notif.SendAsync(vendor.LinkedUserID.Value, "Your vendor account has been approved. Welcome to ProVMS!");

        TempData["Success"] = $"Vendor '{vendor.CompanyName}' approved.";
        return RedirectToAction("AccreditationDesk");
    }

    [HttpPost]
    [Authorize(Policy = "ProcurementOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectVendor(int id)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor == null) return NotFound();

        vendor.OperationalStatus = OperationalStatus.Blacklisted;
        vendor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (vendor.LinkedUserID.HasValue)
            await _notif.SendAsync(vendor.LinkedUserID.Value, "Your vendor application was not approved. Contact procurement for details.");

        TempData["Error"] = $"Vendor '{vendor.CompanyName}' rejected.";
        return RedirectToAction("AccreditationDesk");
    }

    [HttpGet]
    [Authorize(Policy = "DirectoryViewers")]
    public async Task<IActionResult> Directory(string? status, string? search)
    {
        var query = _db.Vendors.Include(v => v.Items).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OperationalStatus>(status, out var s))
            query = query.Where(v => v.OperationalStatus == s);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(v => v.CompanyName.Contains(search) || v.ContactEmail.Contains(search));

        ViewData["Title"] = "Vendor Management";
        ViewData["BreadcrumbModule"] = "Vendor Accreditation";
        var all = await query.Include(v => v.LinkedUser).OrderBy(v => v.CompanyName).ToListAsync();
        ViewBag.Status = status;
        ViewBag.Search = search;
        var topRatedIds = await _db.SupplierEvaluations
            .Where(e => e.VendorID != null)
            .GroupBy(e => e.VendorID)
            .Where(g => g.Average(e => (e.DeliverySpeedStars + e.ItemConditionStars + e.CommunicationStars) / 3.0) >= 4.5)
            .Select(g => g.Key)
            .ToListAsync();
        ViewBag.TopRated = topRatedIds.Count;
        return View(all);
    }

    [HttpGet]
    [Authorize(Policy = "VendorOnly")]
    public async Task<IActionResult> Profile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vendor = await _db.Vendors.Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.LinkedUserID == userId);

        if (vendor == null) return NotFound();
        return View(vendor);
    }

    [HttpPost]
    [Authorize(Policy = "VendorOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItemPrice(int itemId, decimal newPrice)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.LinkedUserID == userId);
        if (vendor == null) return Forbid();

        var item = await _db.VendorItems.FindAsync(itemId);
        if (item == null || item.VendorID != vendor.ID) return Forbid();

        if (newPrice <= 0)
        {
            TempData["Error"] = "Price must be greater than zero.";
            return RedirectToAction("Profile");
        }

        item.UnitPrice = newPrice;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Price updated.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [Authorize(Policy = "ProcurementOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string newStatus)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor == null) return NotFound();

        if (Enum.TryParse<OperationalStatus>(newStatus, out var status))
        {
            vendor.OperationalStatus = status;
            vendor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vendor status updated.";
        }
        return RedirectToAction("Directory");
    }
}
