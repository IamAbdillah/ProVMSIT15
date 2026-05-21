using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProVMSIT15.Models;

public class AppUser
{
    [Key]
    public int ID { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole UserRole { get; set; } = UserRole.User;

    [MaxLength(20)]
    public string? DepartmentCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsArchived { get; set; } = false;

    public ICollection<PurchaseRequisition> Requisitions { get; set; } = new List<PurchaseRequisition>();
    public ICollection<InAppNotification> Notifications { get; set; } = new List<InAppNotification>();
}
