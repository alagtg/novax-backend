namespace YourProject.API.Models.Billing;

public class BillingSettings : YourProject.API.Models.BaseEntity
{
    // Cabinet / prestataire
    public string CompanyName { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Siret { get; set; } = "";
    public string VatNumber { get; set; } = "";
    public string? LogoUrl { get; set; }

    // Banque
    public string AccountHolder { get; set; } = "";
    public string BankName { get; set; } = "";
    public string Agency { get; set; } = "";
    public string Iban { get; set; } = "";
    public string Bic { get; set; } = "";

    // Numérotation
    public string NumberingFormat { get; set; } = "{YYYY}-{SEQ}-C"; // ex: 2026-158-C
    public int AnnualCounter { get; set; } = 1;
    public string? Suffix { get; set; } = "-C";
}
