using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;
using ProVMSIT15.Models;

namespace ProVMSIT15.Controllers;

[Authorize(Policy = "AnalyticsViewers")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Policy = "InternalUsers")]
    public async Task<IActionResult> Index()
    {
        var expensesByDept = await _db.PurchaseRequisitions
            .Include(r => r.Requester)
            .Where(r => r.WorkflowStatus != WorkflowStatus.Pending_Finance && r.WorkflowStatus != WorkflowStatus.Archived)
            .GroupBy(r => r.Requester!.DepartmentCode ?? "Unknown")
            .Select(g => new { Department = g.Key, Total = g.Sum(r => r.TotalCalculatedAmount) })
            .ToListAsync();

        var budgets = await _db.DepartmentBudgets
            .Where(b => b.FiscalYear == DateTime.UtcNow.Year)
            .ToListAsync();

        var vendorScores = await _db.SupplierEvaluations
            .Include(e => e.Vendor)
            .Where(e => e.VendorID != null)
            .GroupBy(e => new { e.VendorID, e.Vendor!.CompanyName })
            .Select(g => new
            {
                VendorName = g.Key.CompanyName,
                Score = g.Average(e => (e.DeliverySpeedStars + e.ItemConditionStars + e.CommunicationStars) / 3.0)
            })
            .OrderByDescending(v => v.Score)
            .Take(10)
            .ToListAsync();

        var pendingVendors = await _db.Vendors.CountAsync(v => v.OperationalStatus == OperationalStatus.PendingVerification);
        var activeVendors = await _db.Vendors.CountAsync(v => v.OperationalStatus == OperationalStatus.Active);
        var pendingReqs = await _db.PurchaseRequisitions.CountAsync(r => r.WorkflowStatus == WorkflowStatus.Pending_Finance);
        var totalReqs = await _db.PurchaseRequisitions.CountAsync();

        ViewBag.ExpenseLabels = expensesByDept.Select(e => e.Department).ToList();
        ViewBag.ExpenseValues = expensesByDept.Select(e => e.Total).ToList();
        ViewBag.BudgetLabels = budgets.Select(b => b.DepartmentCode).ToList();
        ViewBag.BudgetAllocated = budgets.Select(b => b.AllocatedBudget).ToList();
        ViewBag.BudgetRemaining = budgets.Select(b => b.RemainingBudget).ToList();
        ViewBag.VendorScoreLabels = vendorScores.Select(v => v.VendorName).ToList();
        ViewBag.VendorScoreValues = vendorScores.Select(v => Math.Round(v.Score, 2)).ToList();
        ViewBag.PendingVendors = pendingVendors;
        ViewBag.ActiveVendors = activeVendors;
        ViewBag.PendingReqs = pendingReqs;
        ViewBag.TotalReqs = totalReqs;

        return View();
    }

    [HttpGet]
    [Authorize(Policy = "FinanceOnly")]
    public async Task<IActionResult> BudgetManagement()
    {
        var budgets = await _db.DepartmentBudgets
            .Where(b => b.FiscalYear == DateTime.UtcNow.Year)
            .OrderBy(b => b.DepartmentCode)
            .ToListAsync();
        return View(budgets);
    }

    [HttpGet]
    [Authorize(Policy = "AnalyticsViewers")]
    public async Task<IActionResult> Reports()
    {
        ViewData["Title"] = "Reports & Insights";
        ViewData["BreadcrumbModule"] = "Procurement Analytics";
        var totalSpend = await _db.PurchaseRequisitions
            .Where(r => r.WorkflowStatus != WorkflowStatus.Archived)
            .SumAsync(r => r.TotalCalculatedAmount);
        var totalPOs = await _db.PurchaseRequisitions.CountAsync(r => r.WorkflowStatus == WorkflowStatus.PO_Issued || r.WorkflowStatus == WorkflowStatus.In_Transit || r.WorkflowStatus == WorkflowStatus.Delivered);
        var activeVendors = await _db.Vendors.CountAsync(v => v.OperationalStatus == OperationalStatus.Active);
        var avgScore = await _db.SupplierEvaluations.AnyAsync()
            ? await _db.SupplierEvaluations.AverageAsync(e => (e.DeliverySpeedStars + e.ItemConditionStars + e.CommunicationStars) / 3.0)
            : 0;
        var byCategory = await _db.PurchaseRequisitions
            .Include(r => r.Item)
            .Where(r => r.WorkflowStatus != WorkflowStatus.Archived)
            .GroupBy(r => r.Item!.Category)
            .Select(g => new { Category = g.Key.ToString(), Total = g.Sum(r => r.TotalCalculatedAmount) })
            .ToListAsync();
        ViewBag.TotalSpend = totalSpend;
        ViewBag.TotalPOs = totalPOs;
        ViewBag.ActiveVendors = activeVendors;
        ViewBag.AvgScore = avgScore;
        ViewBag.CategoryLabels = byCategory.Select(c => c.Category).ToList();
        ViewBag.CategoryValues = byCategory.Select(c => c.Total).ToList();
        return View();
    }

    [HttpPost]
    [Authorize(Policy = "FinanceOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AllocateBudget(string departmentCode, string departmentName, decimal allocated)
    {
        var existing = await _db.DepartmentBudgets.FirstOrDefaultAsync(
            b => b.DepartmentCode == departmentCode && b.FiscalYear == DateTime.UtcNow.Year);

        if (existing != null)
        {
            existing.AllocatedBudget = allocated;
            existing.DepartmentName = departmentName;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.DepartmentBudgets.Add(new DepartmentBudget
            {
                DepartmentCode = departmentCode,
                DepartmentName = departmentName,
                AllocatedBudget = allocated,
                SpentAmount = 0,
                FiscalYear = DateTime.UtcNow.Year,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Budget for {departmentCode} updated.";
        return RedirectToAction("BudgetManagement");
    }
}
