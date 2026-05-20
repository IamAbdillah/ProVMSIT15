using ProVMSIT15.Data;
using ProVMSIT15.Models;

namespace ProVMSIT15.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SendAsync(int targetUserId, string text)
    {
        var note = new InAppNotification
        {
            TargetUserID = targetUserId,
            NotificationText = text,
            CreatedAt = DateTime.UtcNow
        };
        _db.InAppNotifications.Add(note);
        await _db.SaveChangesAsync();
    }

    public async Task SendToRoleAsync(UserRole role, string text)
    {
        var users = _db.Users.Where(u => u.UserRole == role).ToList();
        foreach (var user in users)
        {
            _db.InAppNotifications.Add(new InAppNotification
            {
                TargetUserID = user.ID,
                NotificationText = text,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }
}
