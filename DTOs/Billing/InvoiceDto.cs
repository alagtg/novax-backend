using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Billing;

public class InvoiceDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public int DossierId { get; set; }
    public string DossierCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string ClientEmail { get; set; } = "";

    public string Number { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }

    public InvoiceStatus Status { get; set; }

    public decimal TotalHt { get; set; }
    public decimal TotalTva { get; set; }
    public decimal TotalTtc { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public PaymentType? PaymentType { get; set; }

    public string? PdfUrl { get; set; }

    public bool EmailSent { get; set; }
    public DateTime? EmailSentAt { get; set; }

    public List<InvoiceLineDto> Lines { get; set; } = new();
}