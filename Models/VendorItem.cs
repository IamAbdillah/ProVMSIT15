using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProVMSIT15.Models;

public class VendorItem
{
    [Key]
    public int ID { get; set; }

    [Required]
    public int VendorID { get; set; }
    public Vendor Vendor { get; set; } = null!;

    [Required, MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    public ItemCategory Category { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitPrice { get; set; }

    public ICollection<PurchaseRequisition> Requisitions { get; set; } = new List<PurchaseRequisition>();
}
