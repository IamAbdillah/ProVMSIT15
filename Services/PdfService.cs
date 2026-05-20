using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using ProVMSIT15.Models;

namespace ProVMSIT15.Services;

public class PdfService
{
    public byte[] GeneratePurchaseOrder(PurchaseRequisition req)
    {
        using var ms = new MemoryStream();
        var writer = new PdfWriter(ms);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        document.Add(new Paragraph("PURCHASE ORDER")
            .SetFontSize(22)
            .SetBold()
            .SetFontColor(ColorConstants.DARK_GRAY)
            .SetTextAlignment(TextAlignment.CENTER));

        document.Add(new Paragraph($"PO Number: PO-{req.ID:D6}")
            .SetFontSize(12).SetBold());
        document.Add(new Paragraph($"Date: {DateTime.UtcNow:MMMM dd, yyyy}")
            .SetFontSize(11));

        document.Add(new Paragraph("\n"));

        document.Add(new Paragraph("ORDER DETAILS").SetBold().SetFontSize(13));

        var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 2, 2 }))
            .UseAllAvailableWidth();

        table.AddHeaderCell(new Cell().Add(new Paragraph("Item").SetBold()));
        table.AddHeaderCell(new Cell().Add(new Paragraph("Qty").SetBold()));
        table.AddHeaderCell(new Cell().Add(new Paragraph("Unit Price").SetBold()));
        table.AddHeaderCell(new Cell().Add(new Paragraph("Total").SetBold()));

        table.AddCell(req.Item?.ItemName ?? "N/A");
        table.AddCell(req.Quantity.ToString());
        table.AddCell($"${req.Item?.UnitPrice:F2}");
        table.AddCell($"${req.TotalCalculatedAmount:F2}");

        document.Add(table);

        document.Add(new Paragraph("\n"));
        document.Add(new Paragraph($"Requester: {req.Requester?.FullName ?? "N/A"}").SetFontSize(11));
        document.Add(new Paragraph($"Department: {req.Requester?.DepartmentCode ?? "N/A"}").SetFontSize(11));
        document.Add(new Paragraph($"Vendor: {req.Item?.Vendor?.CompanyName ?? "N/A"}").SetFontSize(11));
        document.Add(new Paragraph($"Status: {req.WorkflowStatus}").SetFontSize(11).SetBold());

        document.Add(new Paragraph("\n\n"));
        document.Add(new Paragraph("______________________________    ______________________________")
            .SetFontSize(10).SetTextAlignment(TextAlignment.CENTER));
        document.Add(new Paragraph("Procurement Officer Signature         Finance Manager Signature")
            .SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

        document.Close();
        return ms.ToArray();
    }
}
