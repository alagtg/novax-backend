using Microsoft.EntityFrameworkCore;
using YourProject.API.Models;
using YourProject.API.Models.Billing;

namespace YourProject.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<Dossier> Dossiers => Set<Dossier>();
    public DbSet<DossierAssignment> DossierAssignments => Set<DossierAssignment>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<TrackingRow> TrackingRows => Set<TrackingRow>();
    public DbSet<MonthlyFollowUp> MonthlyFollowUps => Set<MonthlyFollowUp>();
    public DbSet<FiscalYear> FiscalYears { get; set; }
    public DbSet<BillingSettings> BillingSettings => Set<BillingSettings>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Dossier code unique per year
        modelBuilder.Entity<Dossier>()
            .HasIndex(d => new { d.Year, d.Code })
            .IsUnique();

        // Assignment: one module per user per dossier
        modelBuilder.Entity<DossierAssignment>()
            .HasIndex(a => new { a.DossierId, a.UserId, a.Module })
            .IsUnique();

        modelBuilder.Entity<DossierAssignment>()
            .HasOne(a => a.Dossier)
            .WithMany(d => d.Assignments)
            .HasForeignKey(a => a.DossierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DossierAssignment>()
            .HasOne(a => a.User)
            .WithMany(u => u.Assignments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkTask>()
            .HasOne(t => t.Dossier)
            .WithMany(d => d.Tasks)
            .HasForeignKey(t => t.DossierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkTask>()
            .HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Decimal precision (évite les warnings de troncature)
        modelBuilder.Entity<WorkTask>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TrackingRow>()
            .HasIndex(r => new { r.Year, r.DossierId, r.AssignedToUserId, r.Module, r.Board })
            .IsUnique();

        modelBuilder.Entity<TrackingRow>()
            .HasOne(r => r.Dossier)
            .WithMany()
            .HasForeignKey(r => r.DossierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrackingRow>()
            .HasOne(r => r.AssignedToUser)
            .WithMany()
            .HasForeignKey(r => r.AssignedToUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MonthlyFollowUp>()
            .HasIndex(f => new { f.Year, f.Month, f.DossierId })
            .IsUnique();

        modelBuilder.Entity<MonthlyFollowUp>()
            .HasOne(f => f.Dossier)
            .WithMany()
            .HasForeignKey(f => f.DossierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalHt).HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalTva).HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalTtc).HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.PaidAmount).HasPrecision(18, 2);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.RemainingAmount).HasPrecision(18, 2);

        modelBuilder.Entity<InvoiceLine>()
            .Property(l => l.Quantity).HasPrecision(18, 4);

        modelBuilder.Entity<InvoiceLine>()
            .Property(l => l.UnitPriceHt).HasPrecision(18, 2);

        modelBuilder.Entity<InvoiceLine>()
            .Property(l => l.VatRate).HasPrecision(5, 2);

        modelBuilder.Entity<InvoiceLine>()
            .Property(l => l.LineHt).HasPrecision(18, 2);

        modelBuilder.Entity<InvoiceLine>()
            .Property(l => l.LineTva).HasPrecision(18, 2);

        modelBuilder.Entity<InvoiceLine>()
            .Property(l => l.LineTtc).HasPrecision(18, 2);
    }
}
