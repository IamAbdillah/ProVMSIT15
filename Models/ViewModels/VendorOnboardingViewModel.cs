using System.ComponentModel.DataAnnotations;

namespace ProVMSIT15.Models.ViewModels;

public class VendorOnboardingViewModel
{
    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string TaxID { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare("Password"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IFormFile? DocumentFile { get; set; }

    public List<VendorItemInputModel> CatalogItems { get; set; } = new();
}

public class VendorItemInputModel
{
    [Required, MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    public ItemCategory Category { get; set; }

    [Required, Range(0.01, 9999999999.99)]
    public decimal UnitPrice { get; set; }
}
