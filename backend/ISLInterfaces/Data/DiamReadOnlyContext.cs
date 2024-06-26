namespace ISLInterfaces.Data;

using ISLInterfaces.Features.CaseAccess;
using ISLInterfaces.Features.Model;
using Microsoft.EntityFrameworkCore;

public class DiamReadOnlyContext : DbContext
{
    public DbSet<SubmittingAgencyRequestModel> SubmittingAgencyRequests { get; set; } = default!;
    public DbSet<PartyModel> Parties { get; set; } = default!;



    private readonly IConfiguration configuration;

    public DiamReadOnlyContext(DbContextOptions<DiamReadOnlyContext> options
       , IConfiguration configuration) : base(options)
    {
        this.configuration = configuration;
    }




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        var schema = this.configuration.GetValue<string>("DatabaseConnectionInfo:Schema");

        modelBuilder.HasDefaultSchema(schema);
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiamReadOnlyContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {


        //  optionsBuilder.UseNpgsql(this.configuration.ConnectionStrings.PidpDatabase, x => x.MigrationsHistoryTable(this.configuration.ConnectionStrings.EfHistoryTable, this.configuration.ConnectionStrings.EfHistorySchema));


        if (Environment.GetEnvironmentVariable("LOG_SQL") != null && "true".Equals(Environment.GetEnvironmentVariable("LOG_SQL"), StringComparison.Ordinal))
        {
            optionsBuilder.LogTo(Console.WriteLine);
        }

    }

}
