using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Billing;

public class UpdateInvoicePaymentRequest
{
    public decimal PaidAmount { get; set; }
    public PaymentType? PaymentType { get; set; }
}