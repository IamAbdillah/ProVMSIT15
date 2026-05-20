using System.ComponentModel.DataAnnotations;

namespace ProVMSIT15.Models;

public class Vendor
{
    [Key]
    public int ID { get; set; }

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string TaxID { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? DocumentVaultURL { get; set; }

    public OperationalStatus OperationalStatus { get; set; } = OperationalStatus.PendingVerification;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    public int? LinkedUserID { get; set; }
    public AppUser? LinkedUser { get; set; }

    public ICollection<VendorItem> Items { get; set; } = new List<VendorItem>();
    public ICollection<SupplierEvaluation> Evaluations { get; set; } = new List<SupplierEvaluation>();
}
