namespace YourProject.API.Models.Billing;

public class InvoiceLine : YourProject.API.Models.BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public string Label { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPriceHt { get; set; } = 0;

    public decimal VatRate { get; set; } = 0; // 0, 7, 19...
    public decimal LineHt { get; set; }
    public decimal LineTva { get; set; }
    public decimal LineTtc { get; set; }
}
