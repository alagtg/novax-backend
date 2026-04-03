using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Billing;

public class SaveInvoiceRequest
{
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public int Month { get; set; } = DateTime.UtcNow.Month;

    public int DossierId { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Brouillon;
    public decimal PaidAmount { get; set; } = 0;

    public PaymentType? PaymentType { get; set; }

    public List<SaveInvoiceLineRequest> Lines { get; set; } = new();
}