using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;
using ProVMSIT15.Models;

namespace ProVMSIT15.Controllers;

[Authorize(Policy = "AnalyticsViewers")]
public class ContractController : Controller
{
    private readonly ApplicationDbContext _db;
    public ContractController(ApplicationDbContext db) { _db = db; }

    // ── CONTRACT LIFECYCLE ──────────────────────────────────────
    public async Task<IActionResult> Lifecycle()
    {
        ViewData["Title"] = "Contract Lifecycle";
        ViewData["BreadcrumbModule"] = "Contract Management";
        var contracts = await _db.Contracts
            .Include(c => c.Vendor)
            .Include(c => c.Items)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(contracts);
    }

    // ── PRICING MANAGEMENT ──────────────────────────────────────
    public async Task<IActionResult> Pricing()
    {
        ViewData["Title"] = "Pricing Management";
        ViewData["BreadcrumbModule"] = "Contract Management";

        var contracts = await _db.Contracts
            .Include(c => c.Vendor)
            .Include(c => c.Items)
                .ThenInclude(ci => ci.VendorItem)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(contracts);
    }

    // ── NEGOTIATIONS ────────────────────────────────────────────
    public async Task<IActionResult> Negotiations()
    {
        ViewData["Title"] = "Negotiations";
        ViewData["BreadcrumbModule"] = "Contract Management";
        var contracts = await _db.Contracts
            .Include(c => c.Vendor)
            .Where(c => c.Status == ContractStatus.UnderNegotiation || c.Status == ContractStatus.Draft)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(contracts);
    }

    // ── CREATE CONTRACT (POST) — C,U: ProcurementOrAdmin only ──
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ProcurementOrAdmin")]
    public async Task<IActionResult> Create(int vendorId, string title, DateTime startDate, DateTime endDate, decimal discount, string? notes)
    {
        var vendor = await _db.Vendors.FindAsync(vendorId);
        if (vendor == null) return NotFound();

        var contract = new Contract
        {
            VendorID = vendorId,
            ContractTitle = title,
            StartDate = startDate,
            EndDate = endDate,
            DiscountPercent = discount,
            NegotiationNotes = notes,
            Status = ContractStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Contract '{title}' created successfully.";
        return RedirectToAction("Lifecycle");
    }

    // ── UPDATE STATUS (POST) — U: ProcurementOrAdmin only ──────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "ProcurementOrAdmin")]
    public async Task<IActionResult> UpdateStatus(int id, ContractStatus status)
    {
        var contract = await _db.Contracts.FindAsync(id);
        if (contract == null) return NotFound();
        contract.Status = status;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Contract status updated.";
        return RedirectToAction("Lifecycle");
    }
}
