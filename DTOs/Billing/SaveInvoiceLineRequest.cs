namespace YourProject.API.DTOs.Billing;

public class SaveInvoiceLineRequest
{
    public string Label { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPriceHt { get; set; } = 0;
    public decimal VatRate { get; set; } = 0;
}
