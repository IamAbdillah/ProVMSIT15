using System.ComponentModel.DataAnnotations;

namespace ProVMSIT15.Models;

public class InAppNotification
{
    [Key]
    public int ID { get; set; }

    [Required]
    public int TargetUserID { get; set; }
    public AppUser TargetUser { get; set; } = null!;

    [Required, MaxLength(500)]
    public string NotificationText { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
