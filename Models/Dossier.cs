namespace YourProject.API.Models;

public class Dossier : BaseEntity
{
    public int Year { get; set; } = DateTime.UtcNow.Year;

    // Identité client / société
    public string Code { get; set; } = ""; // Code interne (ex: CLT-0001)
    public string CompanyName { get; set; } = "";
    public string Sector { get; set; } = "";
    public string ResponsibleName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Siret { get; set; } = "";
    public string VatNumber { get; set; } = "";
    public string Rcs { get; set; } = "";
    public string Fj { get; set; } = "";

    // Adresse
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";

    public string Notes { get; set; } = "";

    // Suivi administratif (tableau global)
    public bool LettreMissionEnvoyee { get; set; } = false;
    public bool MandatEnvoye { get; set; } = false;

    // Navigation
    public ICollection<DossierAssignment> Assignments { get; set; } = new List<DossierAssignment>();
    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
}