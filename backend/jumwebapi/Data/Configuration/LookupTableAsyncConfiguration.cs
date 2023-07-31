//using jumwebapi.Models.Lookups;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;

//namespace jumwebapi.Data.Configuration;

//public class LookupTableAsyncConfiguration<TEntity, TGenerator> : IEntityTypeConfiguration<TEntity>
//    where TEntity : class
//    where TGenerator : ILookupDataGenerator<TEntity>, new()
//{
//    public void Configure(EntityTypeBuilder<TEntity> builder) => builder.HasData(new TGenerator().GenerateAsync());
//}

