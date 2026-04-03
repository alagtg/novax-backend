using YourProject.API.Models.Enums;

namespace YourProject.API.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int Year { get; set; }

        // KPI (sans factures)
        public int TotalDossiers { get; set; }
        public int TotalTasks { get; set; }
        public int TotalRetards { get; set; }
        public int TotalTvaADeclarer { get; set; }
        public int TotalIsAPayer { get; set; }

        public Dictionary<WorkStatus, int> TasksByStatus { get; set; } = new();
        public Dictionary<ModuleType, Dictionary<WorkStatus, int>> TasksByModule { get; set; } = new();

        public List<DashboardCollaboratorPerfDto> PerformanceByCollaboratrice { get; set; } = new();
        public List<DashboardAlertDto> Alerts { get; set; } = new();
        public List<DashboardMonthlyPointDto> MonthlyTasks { get; set; } = new();

        public List<DashboardBoardStatDto> BoardStats { get; set; } = new();
        public List<DashboardLineDto> RecentLines { get; set; } = new();

        public List<DashboardLineDto> Lines { get; set; } = new();
    }

    public class DashboardBoardStatDto
    {
        public ModuleType Module { get; set; }
        public string Board { get; set; } = "Default";
        public int TotalRows { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class DashboardLineDto
    {
        public ModuleType Module { get; set; }
        public string Board { get; set; } = "Default";

        public int DossierId { get; set; }
        public string DossierCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Sector { get; set; } = "";

        public int AssignedToUserId { get; set; }
        public string AssignedToName { get; set; } = "";

        public int? Month { get; set; }
        public string? Periode { get; set; }
        public string? TvaType { get; set; }

        public string? Status { get; set; }
        public decimal? Amount { get; set; }

        public Dictionary<string, object?>? Data { get; set; }

        public bool IsLate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    public class DashboardCollaboratorPerfDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public int Dossiers { get; set; }
        public int Tasks { get; set; }
        public int Retards { get; set; }
    }

    public class DashboardAlertDto
    {
        public string Type { get; set; } = "";
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    public class DashboardMonthlyPointDto
    {
        public int Month { get; set; }
        public decimal Value { get; set; }
    }
}