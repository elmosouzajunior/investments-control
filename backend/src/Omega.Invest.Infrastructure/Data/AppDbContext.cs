using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Omega.Invest.Domain.Entities;
using Omega.Invest.Infrastructure.Identity;

namespace Omega.Invest.Infrastructure.Data;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<InvestmentPlan> InvestmentPlans => Set<InvestmentPlan>();
    public DbSet<InvestmentType> InvestmentTypes => Set<InvestmentType>();
    public DbSet<FinancialInstitution> FinancialInstitutions => Set<FinancialInstitution>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentOperation> InvestmentOperations => Set<InvestmentOperation>();
    public DbSet<InvestmentMonthlyMeasurement> InvestmentMonthlyMeasurements => Set<InvestmentMonthlyMeasurement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.TradeName).HasMaxLength(180);
            entity.Property(x => x.Cnpj).HasMaxLength(14).IsRequired();
            entity.HasIndex(x => x.Cnpj).IsUnique();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(180).IsRequired();
            entity.HasIndex(x => x.CompanyId);
        });

        ConfigureTenantEntity<InvestmentPlan>(builder, "investment_plans");
        builder.Entity<InvestmentPlan>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(600);
            entity.Property(x => x.TargetAmount).HasPrecision(18, 2);
            entity.Property(x => x.TargetTermYears).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        });

        ConfigureTenantEntity<InvestmentType>(builder, "investment_types");
        builder.Entity<InvestmentType>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(400);
            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        });

        ConfigureTenantEntity<FinancialInstitution>(builder, "financial_institutions");
        builder.Entity<FinancialInstitution>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        });

        ConfigureTenantEntity<Investment>(builder, "investments");
        builder.Entity<Investment>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Ticker).HasMaxLength(40);
            entity.Property(x => x.Notes).HasMaxLength(800);
            entity.Property(x => x.InitialAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.ResponsibleUserId);
            entity.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Institution).WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantEntity<InvestmentOperation>(builder, "investment_operations");
        builder.Entity<InvestmentOperation>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasOne(x => x.Investment).WithMany().HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureTenantEntity<InvestmentMonthlyMeasurement>(builder, "investment_monthly_measurements");
        builder.Entity<InvestmentMonthlyMeasurement>(entity =>
        {
            entity.Property(x => x.MarketValue).HasPrecision(18, 2);
            entity.Property(x => x.IncomeAmount).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.CompanyId, x.InvestmentId, x.Month }).IsUnique();
            entity.HasOne(x => x.Investment).WithMany().HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTenantEntity<TEntity>(ModelBuilder builder, string tableName)
        where TEntity : BaseEntity
    {
        builder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(x => x.Id);
            entity.HasOne(typeof(Company), "Company")
                .WithMany()
                .HasForeignKey("CompanyId")
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex("CompanyId");
        });
    }
}
