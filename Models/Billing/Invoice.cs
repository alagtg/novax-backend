using YourProject.API.Models.Enums;

namespace YourProject.API.Models.Billing;

public class Invoice : YourProject.API.Models.BaseEntity
{
    public int Year { get; set; } = DateTime.UtcNow.Year;

    // 1..12
    public int Month { get; set; } = DateTime.UtcNow.Month;

    public int DossierId { get; set; }
    public YourProject.API.Models.Dossier? Dossier { get; set; }

    public string Number { get; set; } = "";
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? DueDate { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Brouillon;

    public decimal TotalHt { get; set; }
    public decimal TotalTva { get; set; }
    public decimal TotalTtc { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public PaymentType? PaymentType { get; set; }

    // PDF facture uploadé
    public string? PdfUrl { get; set; }

    // suivi envoi mail
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}