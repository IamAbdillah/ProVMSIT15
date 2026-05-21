using System.ComponentModel.DataAnnotations;

namespace ProVMSIT15.Models;

public enum SLAWorkflowType
{
    VendorOnboarding,
    FinancialCleardown,
    VendorFulfillment
}

public enum SLABreachStatus
{
    Compliant,
    Breached
}

public class SLAMilestoneLog
{
    [Key]
    public int LogID { get; set; }

    public SLAWorkflowType WorkflowType { get; set; }

    public int ReferenceID { get; set; }

    public DateTime StartTimestamp { get; set; } = DateTime.UtcNow;

    public DateTime? EndTimestamp { get; set; }

    public decimal? DurationHours { get; set; }

    public SLABreachStatus SLABreachStatus { get; set; } = SLABreachStatus.Compliant;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
