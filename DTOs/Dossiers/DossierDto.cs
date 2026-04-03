namespace YourProject.API.DTOs.Dossiers;

public class DossierDto
{
    public int Id { get; set; }
    public int Year { get; set; }
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

    public bool LettreMissionEnvoyee { get; set; }
    public bool MandatEnvoye { get; set; }

    // ✅ assignations actuelles par module
    public int? ComptabiliteUserId { get; set; }
    public string? ComptabiliteUserName { get; set; }

    public int? SocialUserId { get; set; }
    public string? SocialUserName { get; set; }
    public List<int>? SocialUserIds { get; set; }
    public int? JuridiqueUserId { get; set; }
    public string? JuridiqueUserName { get; set; }
}