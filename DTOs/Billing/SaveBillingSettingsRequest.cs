namespace YourProject.API.DTOs.Billing;

public class SaveBillingSettingsRequest
{
    public string CompanyName { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Siret { get; set; } = "";
    public string VatNumber { get; set; } = "";
    public string AccountHolder { get; set; } = "";
    public string BankName { get; set; } = "";
    public string Agency { get; set; } = "";
    public string Iban { get; set; } = "";
    public string Bic { get; set; } = "";
    public string NumberingFormat { get; set; } = "{YYYY}-{SEQ}-C";
    public int AnnualCounter { get; set; }
    public string? Suffix { get; set; }
}
