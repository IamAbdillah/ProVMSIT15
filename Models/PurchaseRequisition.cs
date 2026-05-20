using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProVMSIT15.Models;

public class PurchaseRequisition
{
    [Key]
    public int ID { get; set; }

    [Required]
    public int RequesterID { get; set; }
    public AppUser Requester { get; set; } = null!;

    [Required]
    public int ItemID { get; set; }
    public VendorItem Item { get; set; } = null!;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalCalculatedAmount { get; set; }

    public WorkflowStatus WorkflowStatus { get; set; } = WorkflowStatus.Pending_Finance;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinanceSubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? POIssuedAt { get; set; }
    public bool IsEncumbered { get; set; } = false;

    public SupplierEvaluation? Evaluation { get; set; }
}
