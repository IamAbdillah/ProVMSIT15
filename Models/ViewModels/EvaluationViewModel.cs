using System.ComponentModel.DataAnnotations;

namespace ProVMSIT15.Models.ViewModels;

public class EvaluationViewModel
{
    [Required]
    public int RequisitionID { get; set; }

    [Required, Range(1, 5)]
    public int DeliverySpeedStars { get; set; }

    [Required, Range(1, 5)]
    public int ItemConditionStars { get; set; }

    [Required, Range(1, 5)]
    public int CommunicationStars { get; set; }

    [MaxLength(1000)]
    public string? PerformanceComments { get; set; }

    public string? ItemName { get; set; }
    public string? VendorName { get; set; }
}
