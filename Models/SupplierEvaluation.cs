using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProVMSIT15.Models;

public class SupplierEvaluation
{
    [Key]
    public int ID { get; set; }

    [Required]
    public int RequisitionID { get; set; }
    public PurchaseRequisition Requisition { get; set; } = null!;

    public int? VendorID { get; set; }
    public Vendor? Vendor { get; set; }

    [Required]
    [Range(1, 5)]
    public int DeliverySpeedStars { get; set; }

    [Required]
    [Range(1, 5)]
    public int ItemConditionStars { get; set; }

    [Required]
    [Range(1, 5)]
    public int CommunicationStars { get; set; }

    [NotMapped]
    public double AverageScore => (DeliverySpeedStars + ItemConditionStars + CommunicationStars) / 3.0;

    [MaxLength(1000)]
    public string? PerformanceComments { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
