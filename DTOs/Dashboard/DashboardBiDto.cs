using System;
using System.Collections.Generic;

namespace YourProject.API.DTOs.Dashboard
{
    public class DashboardBiDto
    {
        public int Year { get; set; }
        public string PeriodeType { get; set; } = string.Empty;
        public string PeriodeValue { get; set; } = string.Empty;
        // KPI existants
        public int TotalDossiers { get; set; }
        public int TotalEmployees { get; set; }
        public int EnCours { get; set; }
        public int Fait { get; set; }
        public int Echouee { get; set; }
        public decimal SuccessRate { get; set; }

        // Tableaux existants
        public List<DashboardBiEmployeeDto> Employees { get; set; } = new();
        public List<DashboardBiClientDto> Clients { get; set; } = new();

        // ✅ NOUVEAU : données premium
        public DashboardBiStatusCountsDto StatusCounts { get; set; } = new();
        public List<DashboardBiEmployeeChartDto> EmployeeChart { get; set; } = new();
        public List<DashboardBiModuleChartDto> ModuleChart { get; set; } = new();
        public List<DashboardBiTrendChartDto> TrendChart { get; set; } = new();
        public List<DashboardBiAlertDto> Alerts { get; set; } = new();
    }

    public class DashboardBiEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        public int TotalDossiers { get; set; }

        public int? TvaStatus { get; set; }
        public int? CfeStatus { get; set; }
        public int? IsStatus { get; set; }
        public int? BilanStatus { get; set; }
        public int? InfosGeneralesStatus { get; set; }
        public int? FichesPaieStatus { get; set; }
        public int? DsnStatus { get; set; }
        public int? DpaeStatus { get; set; }
        public int? AgoStatus { get; set; }
        public int? SaisieStatus { get; set; }
        public int? RevisionStatus { get; set; }

        public int EnCours { get; set; }
        public int Fait { get; set; }
        public int Echouee { get; set; }

        public decimal Performance { get; set; }
    }

    public class DashboardBiClientDto
    {
        public int DossierId { get; set; }
        public string DossierCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public int? TvaStatus { get; set; }
        public int? CfeStatus { get; set; }
        public int? IsStatus { get; set; }
        public int? BilanStatus { get; set; }
        public int? InfosGeneralesStatus { get; set; }
        public int? FichesPaieStatus { get; set; }
        public int? DsnStatus { get; set; }
        public int? DpaeStatus { get; set; }

        public int? ComptaStatus { get; set; }
        public int? FiscalStatus { get; set; }
        public int? SocialStatus { get; set; }
        public int? AgoStatus { get; set; }
        public int? SaisieStatus { get; set; }
        public int? RevisionStatus { get; set; }

        public string PeriodeLabel { get; set; } = string.Empty;
        public int? GlobalStatus { get; set; }

        public DateTime? LastUpdate { get; set; }
    }
}