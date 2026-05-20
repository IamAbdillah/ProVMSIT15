using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProVMSIT15.Models;

public class DepartmentBudget
{
    [Key]
    public int ID { get; set; }

    [Required, MaxLength(20)]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(15,2)")]
    public decimal AllocatedBudget { get; set; }

    [Required]
    [Column(TypeName = "decimal(15,2)")]
    public decimal SpentAmount { get; set; } = 0;

    [NotMapped]
    public decimal RemainingBudget => AllocatedBudget - SpentAmount;

    public int FiscalYear { get; set; } = DateTime.UtcNow.Year;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
