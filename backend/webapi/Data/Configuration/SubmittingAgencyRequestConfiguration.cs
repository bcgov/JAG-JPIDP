namespace Pidp.Data.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pidp.Models;

public class SubmittingAgencyRequestConfiguration : IEntityTypeConfiguration<SubmittingAgencyRequest>
{
    public void Configure(EntityTypeBuilder<SubmittingAgencyRequest> builder)
    {
        builder.HasMany(a => a.AgencyRequestAttachments)
            .WithOne(r => r.SubmittingAgencyRequest);
    }
}
