namespace ProVMSIT15.Models;

public class ContractItem
{
    public int ID { get; set; }
    public int ContractID { get; set; }
    public int VendorItemID { get; set; }
    public decimal NegotiatedUnitPrice { get; set; }
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "PHP";

    public Contract? Contract { get; set; }
    public VendorItem? VendorItem { get; set; }
}
