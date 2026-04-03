using System.Text.Json;
using BCrypt.Net;
using YourProject.API.Models;
using YourProject.API.Models.Enums;
using YourProject.API.Models.Billing;

namespace YourProject.API.Data;

public class DbSeeder
{
    private readonly AppDbContext _db;

    public DbSeeder(AppDbContext db)
    {
        _db = db;
    }

    public void Seed()
    {
       SeedUsers();
        SeedBillingSettings();

        // SeedDossiersFromJson(2026);
    }

    // ========================= USERS =========================
    private void SeedUsers()
    {
        if (!_db.Users.Any())
        {
            var users = new List<User>
            {


                new User
                {
                    FullName = "Admin NOVAX",
                    Email = "admin@novax.tn",
                    Role = UserRole.ADMIN,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    IsActive = true
                },
                new User
                {
                    FullName = "Employé 1",
                    Email = "employe1@novax.tn",
                    Role = UserRole.EMPLOYE,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emp123!"),
                    IsActive = true
                },
                new User
                {
                    FullName = "Employé 2",
                    Email = "employe2@novax.tn",
                    Role = UserRole.EMPLOYE,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emp123!"),
                    IsActive = true
                },
                new User
                {
                    FullName = "Comptable Facture",
                    Email = "billing@novax.tn",
                    Role = UserRole.COMPTABLE_FACTURE,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Billing123!"),
                    IsActive = true
                }
            };

            _db.Users.AddRange(users);
            _db.SaveChanges();
        }
    }

    // ========================= BILLING SETTINGS =========================
    private void SeedBillingSettings()
    {
        if (!_db.BillingSettings.Any())
        {
            _db.BillingSettings.Add(new BillingSettings
            {
                CompanyName = "NOVAX - Cabinet",
                Address = "Adresse ...",
                PostalCode = "0000",
                City = "Ville",
                Phone = "+216 00 000 000",
                Email = "contact@novax.tn",
                Siret = "00000000000000",
                VatNumber = "TVA000000",
                AccountHolder = "NOVAX",
                BankName = "Banque ...",
                Agency = "Agence ...",
                Iban = "TN00 0000 0000 0000 0000 0000",
                Bic = "BIC00000",
                NumberingFormat = "{YYYY}-{SEQ}-C",
                AnnualCounter = 1,
                Suffix = "-C"
            });

            _db.SaveChanges();
        }
    }

    // ========================= DOSSIERS =========================
    private void SeedDossiersFromJson(int year)
    {
        var baseDir = AppContext.BaseDirectory;
        var jsonPath = Path.Combine(baseDir, "Data", $"seed.dossiers.{year}.json");

        if (!File.Exists(jsonPath))
            return;

        var json = File.ReadAllText(jsonPath);

        var items = JsonSerializer.Deserialize<List<DossierSeedItem>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        foreach (var i in items)
        {
            if (string.IsNullOrWhiteSpace(i.Code)) continue;

            // ✅ Vérification doublon YEAR + CODE
            bool exists = _db.Dossiers.Any(d =>
                d.Year == i.Year &&
                d.Code == i.Code);

            if (exists) continue;

            var dossier = new Dossier
            {
                Year = i.Year,
                Code = i.Code,
                CompanyName = i.CompanyName ?? "",
                Sector = i.Sector ?? "",
                ResponsibleName = i.ResponsibleName ?? "",
                Phone = i.Phone ?? "",
                Email = i.Email ?? "",
                Siret = i.Siret ?? "",
                VatNumber = i.VatNumber ?? "",
                Address = i.Address ?? "",
                City = i.City ?? "",
                PostalCode = i.PostalCode ?? "",
                Country = i.Country ?? "",
                Notes = i.Notes ?? ""
            };

            _db.Dossiers.Add(dossier);
        }

        _db.SaveChanges();

        AssignDossiers(year);
    }

    // ========================= ASSIGNATION =========================
    private void AssignDossiers(int year)
    {
        var emp1 = _db.Users.FirstOrDefault(u => u.Email == "employe1@novax.tn");
        var emp2 = _db.Users.FirstOrDefault(u => u.Email == "employe2@novax.tn");

        if (emp1 == null || emp2 == null) return;

        var dossiers = _db.Dossiers
            .Where(d => d.Year == year)
            .OrderBy(d => d.Id)
            .ToList();

        for (int idx = 0; idx < dossiers.Count; idx++)
        {
            var target = (idx % 2 == 0) ? emp1 : emp2;

            foreach (var module in new[]
                     { ModuleType.Comptabilite, ModuleType.Social, ModuleType.Juridique })
            {
                bool alreadyAssigned = _db.DossierAssignments.Any(a =>
                    a.DossierId == dossiers[idx].Id &&
                    a.UserId == target.Id &&
                    a.Module == module);

                if (alreadyAssigned) continue;

                _db.DossierAssignments.Add(new DossierAssignment
                {
                    DossierId = dossiers[idx].Id,
                    UserId = target.Id,
                    Module = module
                });
            }
        }

        _db.SaveChanges();
    }

    // ========================= DTO JSON =========================
    private class DossierSeedItem
    {
        public int Year { get; set; }
        public string? Code { get; set; }
        public string? CompanyName { get; set; }
        public string? Sector { get; set; }
        public string? ResponsibleName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Siret { get; set; }
        public string? VatNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Notes { get; set; }
    }
}
