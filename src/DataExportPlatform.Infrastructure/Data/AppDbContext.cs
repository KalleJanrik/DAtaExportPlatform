using DataExportPlatform.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DataExportPlatform.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Costcenter> Costcenters => Set<Costcenter>();
    public DbSet<Accessright> Accessrights => Set<Accessright>();
    public DbSet<CostcenterResponsibleOverride> CostcenterResponsibleOverrides => Set<CostcenterResponsibleOverride>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DepartmentCode).HasMaxLength(20);
        });

        modelBuilder.Entity<Costcenter>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ResponsibleEmail).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<Accessright>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<CostcenterResponsibleOverride>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ResponsibleUserEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.ChangedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000);
        });

        modelBuilder.Entity<PipelineRun>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasMany(x => x.ExportLogs)
             .WithOne(x => x.PipelineRun)
             .HasForeignKey(x => x.PipelineRunId);
        });

        modelBuilder.Entity<ExportLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AppId).HasMaxLength(50).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
