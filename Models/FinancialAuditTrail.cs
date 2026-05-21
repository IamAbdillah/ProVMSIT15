using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProVMSIT15.Models;

public class FinancialAuditTrail
{
    [Key]
    public int AuditID { get; set; }

    [Required, MaxLength(50)]
    public string TransactionType { get; set; } = string.Empty;

    public int RecordID { get; set; }

    public int UserID { get; set; }

    public DateTime SystemTimestamp { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(45)]
    public string MachineIPAddress { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string JWTSignatureHash { get; set; } = string.Empty;

    public string? PayloadBefore { get; set; }

    public string? PayloadAfter { get; set; }

    [ForeignKey(nameof(UserID))]
    public virtual AppUser? Actor { get; set; }
}
