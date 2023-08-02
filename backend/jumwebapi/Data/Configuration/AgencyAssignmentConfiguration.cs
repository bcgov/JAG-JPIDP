using jumwebapi.Data.ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace jumwebapi.Data.Configuration;

public class AgencyAssignmentConfiguration : IEntityTypeConfiguration<JustinAgencyAssignment>
{
    public void Configure(EntityTypeBuilder<JustinAgencyAssignment> builder)
    {
        builder.HasOne(p => p.Agency)
            .WithMany(b => b.AgencyAssignments);
    }
}
