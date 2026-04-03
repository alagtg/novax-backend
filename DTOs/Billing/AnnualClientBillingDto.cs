namespace YourProject.API.DTOs.Billing;

public class AnnualMonthCellDto
{
    public decimal Facture { get; set; }
    public decimal Paye { get; set; }
    public string? PdfUrl { get; set; }
    public int? InvoiceId { get; set; }
}

public class AnnualClientBillingRowDto
{
    public int DossierId { get; set; }
    public string Client { get; set; } = "";
    public string ClientEmail { get; set; } = "";

    public Dictionary<int, AnnualMonthCellDto> Months { get; set; } = new();

    public decimal TotalFacture { get; set; }
    public decimal TotalPaye { get; set; }
    public decimal Ecart { get; set; }
}