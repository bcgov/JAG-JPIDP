namespace Pidp.Data.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pidp.Models.Lookups;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public virtual void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasIndex(x => x.IdpHint)
            .IsUnique();

    }
}
