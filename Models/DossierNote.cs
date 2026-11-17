using System.ComponentModel.DataAnnotations;

namespace YourProject.API.Models;

public class DossierNote : BaseEntity
{
    public int DossierId { get; set; }
    public Dossier Dossier { get; set; } = null!;
    public int AuthorId { get; set; }
    [MaxLength(200)] public string AuthorName { get; set; } = "";
    [MaxLength(160)] public string Title { get; set; } = "";
    [MaxLength(5000)] public string Details { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
}
