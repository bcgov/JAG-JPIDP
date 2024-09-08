namespace MessagingAdapter.Data;

using MessagingAdapter.Models;
using Microsoft.EntityFrameworkCore;


public class MessagingAdapterContext(DbContextOptions<MessagingAdapterContext> options) : DbContext(options)
{

    public DbSet<IdempotentConsumer> IdempotentConsumers { get; set; } = default!;
    public DbSet<PollTracker> PollTrackers { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("messaging-adapter");
        base.OnModelCreating(modelBuilder);

    }

    public bool IsMessageProcessedAlready(string messageId) => this.IdempotentConsumers.Where(ic => ic.MessageId == messageId).Any();

}
