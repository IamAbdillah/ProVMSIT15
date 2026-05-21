using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;
using ProVMSIT15.Models;
using ProVMSIT15.Services;

namespace ProVMSIT15.Controllers;

[Authorize]
public class CatalogController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly NotificationService _notif;
    private readonly BudgetGuardService _budget;
    private readonly PdfService _pdf;
    private readonly AuditService _audit;

    public CatalogController(ApplicationDbContext db, NotificationService notif,
        BudgetGuardService budget, PdfService pdf, AuditService audit)
    {
        _db = db;
        _notif = notif;
        _budget = budget;
        _pdf = pdf;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentDept => User.FindFirstValue("DepartmentCode") ?? "";

    [HttpGet]
    [Authorize(Policy = "RequesterOnly")]
    public async Task<IActionResult> Marketplace(string? category, string? search)
    {
        var query = _db.VendorItems
            .Include(vi => vi.Vendor)
            .Where(vi => vi.Vendor.OperationalStatus == OperationalStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<ItemCategory>(category, out var cat))
            query = query.Where(vi => vi.Category == cat);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(vi => vi.ItemName.Contains(search) || vi.Vendor.CompanyName.Contains(search));

        ViewBag.Category = category;
        ViewBag.Search = search;
        return View(await query.OrderBy(vi => vi.ItemName).ToListAsync());
    }

    [HttpPost]
    [Authorize(Policy = "RequesterOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceRequisition(int itemId, int quantity)
    {
        if (quantity <= 0)
        {
            TempData["Error"] = "Quantity must be at least 1.";
            return RedirectToAction("Marketplace");
        }

        var item = await _db.VendorItems.Include(vi => vi.Vendor).FirstOrDefaultAsync(vi => vi.ID == itemId);
        if (item == null || item.Vendor.OperationalStatus != OperationalStatus.Active)
        {
            TempData["Error"] = "Item not available.";
            return RedirectToAction("Marketplace");
        }

        var total = item.UnitPrice * quantity;
        var dept = CurrentDept;

        var (encumbered, msg) = await _budget.EncumberAsync(dept, total);
        if (!encumbered)
        {
            TempData["Error"] = msg;
            await _notif.SendAsync(CurrentUserId, $"Requisition for '{item.ItemName}' blocked: {msg}");
            Response.StatusCode = 400;
            return RedirectToAction("Marketplace");
        }

        var req = new PurchaseRequisition
        {
            RequesterID = CurrentUserId,
            ItemID = itemId,
            Quantity = quantity,
            TotalCalculatedAmount = total,
            WorkflowStatus = WorkflowStatus.Pending_Finance,
            IsEncumbered = true,
            FinanceSubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.PurchaseRequisitions.Add(req);
        await _db.SaveChangesAsync();

        // AUDIT: PR_Created
        await _audit.LogTransactionAsync("PR_Created", req.ID,
            payloadAfter: new { req.ID, req.RequesterID, req.ItemID, req.Quantity, req.TotalCalculatedAmount, Status = "Pending_Finance" });

        // SLA: FinancialCleardown timer starts now
        await _audit.OpenSLAAsync(SLAWorkflowType.FinancialCleardown, req.ID);

        await _notif.SendAsync(CurrentUserId, $"Requisition submitted for '{item.ItemName}' x{quantity}. ₱{total:N2} pre-encumbered. Awaiting Finance approval.");
        await _notif.SendToRoleAsync(UserRole.Finance, $"🔔 PR #{req.ID} — '{item.ItemName}' requires immediate budget clearance review.");

        TempData["Success"] = $"Requisition submitted. ₱{total:N2} encumbered from department budget."
            + " Awaiting Finance approval.";
        return RedirectToAction("MyRequests");
    }

    [HttpGet]
    [Authorize(Policy = "RequesterOnly")]
    public async Task<IActionResult> MyRequests()
    {
        var userId = CurrentUserId;
        var reqs = await _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Requester)
            .Include(r => r.Evaluation)
            .Where(r => r.RequesterID == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(reqs);
    }

    [HttpGet]
    [Authorize(Policy = "FinanceOnly")]
    public async Task<IActionResult> ApprovalWorkflow()
    {
        var reqs = await _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Requester)
            .Where(r => r.WorkflowStatus == WorkflowStatus.Pending_Finance)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(reqs);
    }

    [HttpPost]
    [Authorize(Policy = "FinanceOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBudget(int id)
    {
        var req = await _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.ID == id);

        if (req == null) return NotFound();

        var beforeApprove = new { req.ID, PreviousStatus = req.WorkflowStatus.ToString() };
        req.WorkflowStatus = WorkflowStatus.Approved_Budget;
        req.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // AUDIT: PR_Approved
        await _audit.LogTransactionAsync("PR_Approved", req.ID,
            payloadBefore: beforeApprove,
            payloadAfter: new { req.ID, Status = "Approved_Budget", ApprovedAt = req.ApprovedAt });

        // SLA: FinancialCleardown closes (24h limit)
        await _audit.CloseSLAAsync(SLAWorkflowType.FinancialCleardown, req.ID, 24m);

        await _notif.SendAsync(req.RequesterID, $"✅ Budget approved for PR #{req.ID}. Procurement will issue the PO shortly.");
        await _notif.SendToRoleAsync(UserRole.Procurement, $"🔔 PR #{req.ID} approved by Finance. Issue Purchase Order now.");

        TempData["Success"] = $"PR #{req.ID} approved. PO queued for Procurement."
;
        return RedirectToAction("ApprovalWorkflow");
    }

    [HttpPost]
    [Authorize(Policy = "FinanceOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequisition(int id)
    {
        var req = await _db.PurchaseRequisitions.Include(r => r.Requester).FirstOrDefaultAsync(r => r.ID == id);
        if (req == null) return NotFound();

        var dept = req.Requester?.DepartmentCode ?? "";
        if (req.IsEncumbered)
            await _budget.RestoreBudgetAsync(dept, req.TotalCalculatedAmount);

        var beforeReject = new { req.ID, PreviousStatus = req.WorkflowStatus.ToString() };
        req.WorkflowStatus = WorkflowStatus.Archived;
        req.IsEncumbered = false;
        await _db.SaveChangesAsync();

        // AUDIT: PR_Rejected
        await _audit.LogTransactionAsync("PR_Rejected", req.ID,
            payloadBefore: beforeReject,
            payloadAfter: new { req.ID, Status = "Archived", Reason = "Finance rejection", BudgetReleased = req.TotalCalculatedAmount });

        // SLA: FinancialCleardown closes (24h limit)
        await _audit.CloseSLAAsync(SLAWorkflowType.FinancialCleardown, req.ID, 24m);

        await _notif.SendAsync(req.RequesterID, $"❌ PR #{req.ID} rejected by Finance. ₱{req.TotalCalculatedAmount:N2} released back to department budget.");

        TempData["Error"] = $"PR #{req.ID} rejected. Budget restored.";
        return RedirectToAction("ApprovalWorkflow");
    }

    [HttpGet]
    [Authorize(Policy = "POVaultViewers")]
    public async Task<IActionResult> POVault()
    {
        var query = _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Requester)
            .Where(r => r.WorkflowStatus == WorkflowStatus.Approved_Budget ||
                        r.WorkflowStatus == WorkflowStatus.PO_Issued ||
                        r.WorkflowStatus == WorkflowStatus.In_Transit ||
                        r.WorkflowStatus == WorkflowStatus.Delivered)
            .AsQueryable();

        if (User.IsInRole("User"))
            query = query.Where(r => r.RequesterID == CurrentUserId);
        else if (User.IsInRole("Vendor"))
        {
            var vendorEmail = User.Identity!.Name;
            query = query.Where(r => r.Item!.Vendor!.ContactEmail == vendorEmail);
        }

        return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
    }

    [HttpPost]
    [Authorize(Policy = "ProcurementOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuePO(int id)
    {
        var req = await _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.ID == id);

        if (req == null) return NotFound();

        var beforePO = new { req.ID, PreviousStatus = req.WorkflowStatus.ToString() };
        req.WorkflowStatus = WorkflowStatus.PO_Issued;
        req.POIssuedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // AUDIT: PO_Issued
        await _audit.LogTransactionAsync("PO_Issued", req.ID,
            payloadBefore: beforePO,
            payloadAfter: new { req.ID, Status = "PO_Issued", POIssuedAt = req.POIssuedAt });

        // SLA: VendorFulfillment timer starts now (72h)
        await _audit.OpenSLAAsync(SLAWorkflowType.VendorFulfillment, req.ID);

        if (req.Item?.Vendor?.LinkedUserID.HasValue == true)
            await _notif.SendAsync(req.Item.Vendor.LinkedUserID!.Value, $"Purchase Order PO-{req.ID:D6} has been issued to you. Please fulfill.");

        await _notif.SendAsync(req.RequesterID, $"PO-{req.ID:D6} issued. Awaiting delivery.");

        TempData["Success"] = $"PO-{req.ID:D6} issued.";
        return RedirectToAction("POVault");
    }

    [HttpGet]
    [Authorize(Policy = "POVaultViewers")]
    public async Task<IActionResult> DownloadPO(int id)
    {
        var req = await _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i.Vendor)
            .Include(r => r.Requester)
            .FirstOrDefaultAsync(r => r.ID == id);

        if (req == null) return NotFound();

        var bytes = _pdf.GeneratePurchaseOrder(req);
        return File(bytes, "application/pdf", $"PO-{req.ID:D6}.pdf");
    }

    [HttpPost]
    [Authorize(Policy = "VendorOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCargoStatus(int id)
    {
        var userId = CurrentUserId;
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.LinkedUserID == userId);
        if (vendor == null) return Forbid();

        var req = await _db.PurchaseRequisitions
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.ID == id && r.Item.VendorID == vendor.ID);

        if (req == null) return Forbid();

        if (req.WorkflowStatus != WorkflowStatus.PO_Issued)
        {
            TempData["Error"] = "Can only transition from PO_Issued status.";
            return RedirectToAction("VendorOrders");
        }

        var beforeTransit = new { req.ID, PreviousStatus = req.WorkflowStatus.ToString() };
        req.WorkflowStatus = WorkflowStatus.In_Transit;
        await _db.SaveChangesAsync();

        // AUDIT: In_Transit + SLA: VendorFulfillment closes (72h limit)
        await _audit.LogTransactionAsync("Cargo_Dispatched", req.ID,
            payloadBefore: beforeTransit,
            payloadAfter: new { req.ID, Status = "In_Transit", DispatchedAt = DateTime.UtcNow });
        await _audit.CloseSLAAsync(SLAWorkflowType.VendorFulfillment, req.ID, 72m);

        await _notif.SendAsync(req.RequesterID, $"Your order #{req.ID} is now In Transit!");

        TempData["Success"] = "Order marked as In Transit.";
        return RedirectToAction("VendorOrders");
    }

    [HttpGet]
    [Authorize(Policy = "VendorOnly")]
    public async Task<IActionResult> VendorOrders()
    {
        var userId = CurrentUserId;
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.LinkedUserID == userId);
        if (vendor == null) return Forbid();

        var reqs = await _db.PurchaseRequisitions
            .Include(r => r.Item)
            .Include(r => r.Requester)
            .Where(r => r.Item.VendorID == vendor.ID && r.WorkflowStatus != WorkflowStatus.Pending_Finance)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(reqs);
    }

    [HttpPost]
    [Authorize(Policy = "ProcurementOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(int id)
    {
        var req = await _db.PurchaseRequisitions.FindAsync(id);
        if (req == null) return NotFound();

        var beforeDelivered = new { req.ID, PreviousStatus = req.WorkflowStatus.ToString() };
        req.WorkflowStatus = WorkflowStatus.Delivered;
        await _db.SaveChangesAsync();

        // AUDIT: Fulfillment_Settled
        await _audit.LogTransactionAsync("Fulfillment_Settled", req.ID,
            payloadBefore: beforeDelivered,
            payloadAfter: new { req.ID, Status = "Delivered", ConfirmedAt = DateTime.UtcNow });

        await _notif.SendAsync(req.RequesterID, $"✅ Order #{req.ID} confirmed received. Please complete your mandatory supplier evaluation.");

        TempData["Success"] = $"Receipt confirmed for PO-{req.ID:D6}. Complete the mandatory evaluation below.";
        return RedirectToAction("Submit", "Evaluation", new { requisitionId = req.ID });
    }

    // Receipt Confirmation: User C,R — user confirms their own order received
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequesterOnly")]
    public async Task<IActionResult> ConfirmReceipt(int id)
    {
        var req = await _db.PurchaseRequisitions.FindAsync(id);
        if (req == null) return NotFound();
        if (req.RequesterID != CurrentUserId) return Forbid();
        if (req.WorkflowStatus != WorkflowStatus.In_Transit)
        {
            TempData["Error"] = "Order must be In Transit to confirm receipt.";
            return RedirectToAction("MyRequests");
        }
        req.WorkflowStatus = WorkflowStatus.Delivered;
        await _db.SaveChangesAsync();
        await _audit.LogTransactionAsync("Receipt_Confirmed", req.ID,
            payloadAfter: new { req.ID, Status = "Delivered", ConfirmedBy = CurrentUserId, ConfirmedAt = DateTime.UtcNow });
        await _notif.SendAsync(req.RequesterID, $"✅ You confirmed receipt of Order #{req.ID}. Please submit your evaluation.");
        TempData["Success"] = $"Receipt confirmed. Please evaluate the vendor.";  
        return RedirectToAction("Submit", "Evaluation", new { requisitionId = req.ID });
    }

    [HttpGet]
    [Authorize(Policy = "DeliveryViewers")]
    public async Task<IActionResult> DeliveryTracking()
    {
        ViewData["Title"] = "Delivery Tracking";
        ViewData["BreadcrumbModule"] = "Purchase Requisition";
        var query = _db.PurchaseRequisitions
            .Include(r => r.Item).ThenInclude(i => i!.Vendor)
            .Include(r => r.Requester)
            .Where(r => r.WorkflowStatus == WorkflowStatus.PO_Issued || r.WorkflowStatus == WorkflowStatus.In_Transit)
            .AsQueryable();
        if (User.IsInRole("Vendor"))
        {
            var email = User.Identity!.Name;
            query = query.Where(r => r.Item!.Vendor!.ContactEmail == email);
        }
        else if (!User.IsInRole("Admin") && !User.IsInRole("Procurement"))
        {
            query = query.Where(r => r.RequesterID == CurrentUserId);
        }
        return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "VendorOnly")]
    public async Task<IActionResult> MarkInTransit(int id)
    {
        var req = await _db.PurchaseRequisitions.Include(r => r.Item).ThenInclude(i => i!.Vendor).FirstOrDefaultAsync(r => r.ID == id);
        if (req == null) return NotFound();
        var email = User.Identity!.Name;
        if (req.Item?.Vendor?.ContactEmail != email) return Forbid();
        var beforeTransit2 = new { req.ID, PreviousStatus = req.WorkflowStatus.ToString() };
        req.WorkflowStatus = WorkflowStatus.In_Transit;
        await _db.SaveChangesAsync();
        await _audit.LogTransactionAsync("Cargo_Dispatched", req.ID,
            payloadBefore: beforeTransit2,
            payloadAfter: new { req.ID, Status = "In_Transit", DispatchedAt = DateTime.UtcNow });
        await _audit.CloseSLAAsync(SLAWorkflowType.VendorFulfillment, req.ID, 72m);
        await _notif.SendAsync(req.RequesterID, $"Order #{req.ID} is now In Transit!");
        TempData["Success"] = "Order marked as In Transit.";
        return RedirectToAction("DeliveryTracking");
    }

    [HttpGet]
    [Authorize(Policy = "RequesterOnly")]
    public async Task<IActionResult> GetItemPrice(int itemId)
    {
        var item = await _db.VendorItems.FindAsync(itemId);
        if (item == null) return NotFound();
        return Json(new { unitPrice = item.UnitPrice });
    }
}
