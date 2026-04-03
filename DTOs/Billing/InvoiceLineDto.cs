namespace YourProject.API.DTOs.Billing;

public class InvoiceLineDto
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPriceHt { get; set; }
    public decimal VatRate { get; set; }
    public decimal LineHt { get; set; }
    public decimal LineTva { get; set; }
    public decimal LineTtc { get; set; }
}
