using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;
using ProVMSIT15.Models;
using ProVMSIT15.Models.ViewModels;
using ProVMSIT15.Services;

namespace ProVMSIT15.Controllers;

[Authorize]
public class EvaluationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationService _notif;

    public EvaluationController(ApplicationDbContext db, NotificationService notif)
    {
        _db = db;
        _notif = notif;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Submit(int requisitionId)
    {
        var req = await _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Evaluation)
            .FirstOrDefaultAsync(r => r.ID == requisitionId);

        if (req == null) return NotFound();
        if (req.RequesterID != CurrentUserId) return Forbid();
        if (req.WorkflowStatus != WorkflowStatus.Delivered)
        {
            TempData["Error"] = "Evaluation only available for delivered orders.";
            return RedirectToAction("MyRequests", "Catalog");
        }
        if (req.Evaluation != null)
        {
            TempData["Error"] = "You have already evaluated this order.";
            return RedirectToAction("MyRequests", "Catalog");
        }

        var vm = new EvaluationViewModel
        {
            RequisitionID = requisitionId,
            ItemName = req.Item?.ItemName,
            VendorName = req.Item?.Vendor?.CompanyName
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(EvaluationViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var req = await _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Evaluation)
            .FirstOrDefaultAsync(r => r.ID == model.RequisitionID);

        if (req == null) return NotFound();
        if (req.RequesterID != CurrentUserId) return Forbid();
        if (req.WorkflowStatus != WorkflowStatus.Delivered || req.Evaluation != null)
        {
            TempData["Error"] = "Invalid evaluation request.";
            return RedirectToAction("MyRequests", "Catalog");
        }

        var eval = new SupplierEvaluation
        {
            RequisitionID = req.ID,
            VendorID = req.Item?.VendorID,
            DeliverySpeedStars = model.DeliverySpeedStars,
            ItemConditionStars = model.ItemConditionStars,
            CommunicationStars = model.CommunicationStars,
            PerformanceComments = model.PerformanceComments,
            CreatedDate = DateTime.UtcNow
        };

        _db.SupplierEvaluations.Add(eval);
        req.WorkflowStatus = WorkflowStatus.Archived;
        await _db.SaveChangesAsync();

        if (req.Item?.Vendor?.LinkedUserID.HasValue == true)
        {
            var avg = eval.AverageScore;
            await _notif.SendAsync(req.Item.Vendor.LinkedUserID!.Value,
                $"You received a {avg:F1}/5 star evaluation for order #{req.ID}.");
        }

        TempData["Success"] = "Evaluation submitted. Thank you!";
        return RedirectToAction("MyRequests", "Catalog");
    }

    [HttpGet]
    [Authorize(Policy = "ProcurementOrAdmin")]
    public async Task<IActionResult> Leaderboard()
    {
        var vendorScores = await _db.SupplierEvaluations
            .Include(e => e.Vendor)
            .Where(e => e.VendorID != null)
            .GroupBy(e => new { e.VendorID, e.Vendor!.CompanyName })
            .Select(g => new VendorScoreModel
            {
                VendorID = g.Key.VendorID!.Value,
                CompanyName = g.Key.CompanyName,
                EvaluationCount = g.Count(),
                AvgDelivery = g.Average(e => e.DeliverySpeedStars),
                AvgCondition = g.Average(e => e.ItemConditionStars),
                AvgCommunication = g.Average(e => e.CommunicationStars),
                OverallAverage = g.Average(e => (e.DeliverySpeedStars + e.ItemConditionStars + e.CommunicationStars) / 3.0)
            })
            .OrderByDescending(v => v.OverallAverage)
            .ToListAsync();

        var flagged = vendorScores.Where(v => v.OverallAverage < 2.5).ToList();
        foreach (var low in flagged)
        {
            var vendor = await _db.Vendors.FindAsync(low.VendorID);
            if (vendor != null && vendor.OperationalStatus == OperationalStatus.Active)
            {
                await _notif.SendToRoleAsync(UserRole.Admin,
                    $"ALERT: Vendor '{vendor.CompanyName}' has a critically low score ({low.OverallAverage:F1}/5).");
            }
        }

        return View(vendorScores);
    }

    [HttpGet, Authorize(Policy = "ProcurementOrAdmin")]
    public async Task<IActionResult> Performance()
    {
        ViewData["Title"] = "Performance Evaluation";
        ViewData["BreadcrumbModule"] = "Supplier Evaluation";
        var vendorScores = await _db.SupplierEvaluations
            .Include(e => e.Vendor)
            .Where(e => e.VendorID != null)
            .GroupBy(e => new { e.VendorID, e.Vendor!.CompanyName })
            .Select(g => new VendorScoreModel
            {
                VendorID = g.Key.VendorID!.Value,
                CompanyName = g.Key.CompanyName,
                EvaluationCount = g.Count(),
                AvgDelivery = g.Average(e => (double)e.DeliverySpeedStars),
                AvgCondition = g.Average(e => (double)e.ItemConditionStars),
                AvgCommunication = g.Average(e => (double)e.CommunicationStars),
                OverallAverage = g.Average(e => (e.DeliverySpeedStars + e.ItemConditionStars + e.CommunicationStars) / 3.0)
            })
            .OrderByDescending(v => v.OverallAverage)
            .ToListAsync();
        return View(vendorScores);
    }

    [HttpGet, Authorize(Policy = "ProcurementOrAdmin")]
    public async Task<IActionResult> Benchmarking()
    {
        ViewData["Title"] = "Benchmarking";
        ViewData["BreadcrumbModule"] = "Supplier Evaluation";
        var vendorScores = await _db.SupplierEvaluations
            .Include(e => e.Vendor)
            .Where(e => e.VendorID != null)
            .GroupBy(e => new { e.VendorID, e.Vendor!.CompanyName })
            .Select(g => new VendorScoreModel
            {
                VendorID = g.Key.VendorID!.Value,
                CompanyName = g.Key.CompanyName,
                EvaluationCount = g.Count(),
                AvgDelivery = g.Average(e => (double)e.DeliverySpeedStars),
                AvgCondition = g.Average(e => (double)e.ItemConditionStars),
                AvgCommunication = g.Average(e => (double)e.CommunicationStars),
                OverallAverage = g.Average(e => (e.DeliverySpeedStars + e.ItemConditionStars + e.CommunicationStars) / 3.0)
            })
            .OrderByDescending(v => v.OverallAverage)
            .ToListAsync();
        return View(vendorScores);
    }
}

public class VendorScoreModel
{
    public int VendorID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public double AvgDelivery { get; set; }
    public double AvgCondition { get; set; }
    public double AvgCommunication { get; set; }
    public double OverallAverage { get; set; }
}
