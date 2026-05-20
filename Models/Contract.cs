namespace ProVMSIT15.Models;

public class Contract
{
    public int ID { get; set; }
    public int VendorID { get; set; }
    public string ContractTitle { get; set; } = string.Empty;
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalValue { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? NegotiationNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Vendor? Vendor { get; set; }
    public ICollection<ContractItem> Items { get; set; } = new List<ContractItem>();
}
