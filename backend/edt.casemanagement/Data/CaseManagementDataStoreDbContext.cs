namespace edt.casemanagement.Data;

using edt.casemanagement.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

public class CaseManagementDataStoreDbContext : DbContext
{
    private readonly IClock clock;

    public CaseManagementDataStoreDbContext(DbContextOptions<CaseManagementDataStoreDbContext> options, IClock clock) : base(options) => this.clock = clock;

    public DbSet<CaseRequest> CaseRequests { get; set; } = default!;
    public DbSet<CaseSearchRequest> CaseSearchRequests { get; set; } = default!;


    public override int SaveChanges()
    {
        this.ApplyAudits();

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.ApplyAudits();

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("casemgmt");
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CaseManagementDataStoreDbContext).Assembly);
    }

    private void ApplyAudits()
    {
        this.ChangeTracker.DetectChanges();
        var updated = this.ChangeTracker.Entries()
            .Where(x => x.Entity is BaseAuditable
                && (x.State == EntityState.Added || x.State == EntityState.Modified));

        var currentInstant = this.clock.GetCurrentInstant();

        foreach (var entry in updated)
        {
            entry.CurrentValues[nameof(BaseAuditable.Modified)] = currentInstant;

            if (entry.State == EntityState.Added)
            {
                entry.CurrentValues[nameof(BaseAuditable.Created)] = currentInstant;
            }
            else
            {
                entry.Property(nameof(BaseAuditable.Created)).IsModified = false;
            }
        }
    }


}
