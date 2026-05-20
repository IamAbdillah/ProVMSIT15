using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProVMSIT15.Data;

namespace ProVMSIT15.Controllers;

[Authorize]
[Route("api/[controller]")]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _db.InAppNotifications
            .CountAsync(n => n.TargetUserID == CurrentUserId && !n.IsRead);
        return Json(new { count });
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var notes = await _db.InAppNotifications
            .Where(n => n.TargetUserID == CurrentUserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new { n.ID, n.NotificationText, n.IsRead, n.CreatedAt })
            .ToListAsync();
        return Json(notes);
    }

    [HttpPost("mark-read/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var note = await _db.InAppNotifications
            .FirstOrDefaultAsync(n => n.ID == id && n.TargetUserID == CurrentUserId);
        if (note == null) return NotFound();

        note.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("mark-all-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var notes = await _db.InAppNotifications
            .Where(n => n.TargetUserID == CurrentUserId && !n.IsRead)
            .ToListAsync();
        foreach (var note in notes) note.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
