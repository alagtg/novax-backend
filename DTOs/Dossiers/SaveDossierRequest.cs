namespace YourProject.API.DTOs.Dossiers;

public class SaveDossierRequest
{
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public string Code { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Sector { get; set; } = "";
    public string ResponsibleName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Siret { get; set; } = "";
    public string VatNumber { get; set; } = "";
    public string Rcs { get; set; } = "";
    public string Fj { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
    public string Notes { get; set; } = "";

    public bool LettreMissionEnvoyee { get; set; } = false;
    public bool MandatEnvoye { get; set; } = false;
}
