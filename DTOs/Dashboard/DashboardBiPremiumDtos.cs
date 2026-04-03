using System.Collections.Generic;

namespace YourProject.API.DTOs.Dashboard
{
    public class DashboardBiStatusCountsDto
    {
        public int EnCours { get; set; }
        public int Fait { get; set; }
        public int Echouee { get; set; }
    }

    public class DashboardBiEmployeeChartDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalDossiers { get; set; }
        public int EnCours { get; set; }
        public int Fait { get; set; }
        public int Echouee { get; set; }
        public decimal Performance { get; set; }
    }

    public class DashboardBiModuleChartDto
    {
        public string Module { get; set; } = string.Empty;
        public int Total { get; set; }
        public int EnCours { get; set; }
        public int Fait { get; set; }
        public int Echouee { get; set; }
    }

    public class DashboardBiTrendChartDto
    {
        public string Label { get; set; } = string.Empty; // Janvier, Février, T1...
        public int EnCours { get; set; }
        public int Fait { get; set; }
        public int Echouee { get; set; }
    }

    public class DashboardBiAlertDto
    {
        public string Type { get; set; } = string.Empty;     // client, employee, module...
        public string Level { get; set; } = "info";          // info, warning, critical
        public string Message { get; set; } = string.Empty;

        public int? DossierId { get; set; }
        public int? EmployeeId { get; set; }
    }
}