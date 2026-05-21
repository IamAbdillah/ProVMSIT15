using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProVMSIT15.Data;
using ProVMSIT15.Models;

namespace ProVMSIT15.Services;

public class AuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    // ── FINANCIAL AUDIT TRAIL ────────────────────────────────────────────
    public async Task LogTransactionAsync(
        string transactionType,
        int recordId,
        object? payloadBefore = null,
        object? payloadAfter = null)
    {
        var ctx = _http.HttpContext;
        var userId = GetUserId(ctx);
        var ip = GetClientIp(ctx);
        var jwtHash = GetJwtHash(ctx);

        var entry = new FinancialAuditTrail
        {
            TransactionType  = transactionType,
            RecordID         = recordId,
            UserID           = userId,
            SystemTimestamp  = DateTime.UtcNow,
            MachineIPAddress = ip,
            JWTSignatureHash = jwtHash,
            PayloadBefore    = payloadBefore is null ? null : JsonSerializer.Serialize(payloadBefore, _json),
            PayloadAfter     = payloadAfter  is null ? null : JsonSerializer.Serialize(payloadAfter,  _json)
        };

        _db.FinancialAuditTrails.Add(entry);
        await _db.SaveChangesAsync();
    }

    // ── SLA MILESTONE: OPEN ───────────────────────────────────────────────
    public async Task OpenSLAAsync(SLAWorkflowType type, int referenceId)
    {
        var existing = _db.SLAMilestoneLogs
            .FirstOrDefault(s => s.WorkflowType == type &&
                                 s.ReferenceID  == referenceId &&
                                 s.EndTimestamp == null);
        if (existing != null) return;

        _db.SLAMilestoneLogs.Add(new SLAMilestoneLog
        {
            WorkflowType   = type,
            ReferenceID    = referenceId,
            StartTimestamp = DateTime.UtcNow,
            SLABreachStatus = SLABreachStatus.Compliant,
            UpdatedDate    = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // ── SLA MILESTONE: CLOSE ─────────────────────────────────────────────
    // slaLimitHours: 48 = VendorOnboarding, 24 = FinancialCleardown, 72 = VendorFulfillment
    public async Task CloseSLAAsync(SLAWorkflowType type, int referenceId, decimal slaLimitHours)
    {
        var log = _db.SLAMilestoneLogs
            .Where(s => s.WorkflowType == type &&
                        s.ReferenceID  == referenceId &&
                        s.EndTimestamp == null)
            .OrderByDescending(s => s.StartTimestamp)
            .FirstOrDefault();

        if (log == null) return;

        var end = DateTime.UtcNow;
        var hours = (decimal)(end - log.StartTimestamp).TotalHours;

        log.EndTimestamp    = end;
        log.DurationHours   = Math.Round(hours, 2);
        log.SLABreachStatus = hours > slaLimitHours ? SLABreachStatus.Breached : SLABreachStatus.Compliant;
        log.UpdatedDate     = end;

        await _db.SaveChangesAsync();
    }

    // ── HELPERS ───────────────────────────────────────────────────────────
    private static int GetUserId(HttpContext? ctx)
    {
        var val = ctx?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(val, out var id) ? id : 0;
    }

    private static string GetClientIp(HttpContext? ctx)
    {
        if (ctx == null) return "0.0.0.0";
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
    }

    private static string GetJwtHash(HttpContext? ctx)
    {
        if (ctx == null) return string.Empty;
        var token = ctx.Request.Cookies[".AspNetCore.ProVMS"]
                 ?? ctx.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token)) return "no-token";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
