using Microsoft.EntityFrameworkCore;

namespace KnOwl.EntityFrameworkCore;

/// <summary>
/// Provides advanced Entity Framework Core model configuration for KnOwl.
/// </summary>
public static class KnOwlModelBuilderExtensions
{
    /// <summary>
    /// Adds the KnOwl inbox table and indexes to the model when configuring a model manually.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="tableName">The inbox table name.</param>
    /// <param name="schema">The optional table schema.</param>
    /// <returns>The same model builder for fluent configuration.</returns>
    public static ModelBuilder ApplyKnOwlInbox(
        this ModelBuilder modelBuilder,
        string tableName = "KnOwlInboxEntries",
        string schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<KnOwlInboxEntryRecord>(entity =>
        {
            entity.ToTable(tableName, schema);
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => new { entry.Source, entry.Operation, entry.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_KnOwlInbox_Source_Operation_Key");
            entity.Property(entry => entry.Id).HasMaxLength(26).IsRequired();
            entity.Property(entry => entry.Source).HasMaxLength(128).IsRequired();
            entity.Property(entry => entry.Operation).HasMaxLength(512).IsRequired();
            entity.Property(entry => entry.IdempotencyKey).HasMaxLength(256).IsRequired();
            entity.Property(entry => entry.IdempotencyKeySource).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.PayloadHash).HasMaxLength(128);
            entity.Property(entry => entry.PayloadType).HasMaxLength(1024);
            entity.Property(entry => entry.CorrelationId).HasMaxLength(256);
            entity.Property(entry => entry.Status).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.ExecutionMode).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.MetadataJson).HasColumnType("nvarchar(max)");
            entity.Property(entry => entry.CompletionJson).HasColumnType("nvarchar(max)");
            entity.Property(entry => entry.FailureDetailsJson).HasColumnType("nvarchar(max)");
            entity.Property(entry => entry.Failure).HasColumnType("nvarchar(max)");
        });

        return modelBuilder;
    }

    /// <summary>
    /// Adds the KnOwl outbox table and indexes to the model when configuring a model manually.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="tableName">The outbox table name.</param>
    /// <param name="schema">The optional table schema.</param>
    /// <returns>The same model builder for fluent configuration.</returns>
    public static ModelBuilder ApplyKnOwlOutbox(
        this ModelBuilder modelBuilder,
        string tableName = "KnOwlOutboxEnvelopes",
        string schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<KnOwlOutboxEnvelopeRecord>(entity =>
        {
            entity.ToTable(tableName, schema);
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => entry.Status).HasDatabaseName("IX_KnOwlOutbox_Status");
            entity.HasIndex(entry => entry.Transport).HasDatabaseName("IX_KnOwlOutbox_Transport");
            entity.HasIndex(entry => entry.Operation).HasDatabaseName("IX_KnOwlOutbox_Operation");
            entity.HasIndex(entry => entry.CorrelationId).HasDatabaseName("IX_KnOwlOutbox_CorrelationId");
            entity.HasIndex(entry => entry.CreatedOnUtc).HasDatabaseName("IX_KnOwlOutbox_CreatedOnUtc");
            entity.Property(entry => entry.Id).HasMaxLength(26).IsRequired();
            entity.Property(entry => entry.Transport).HasMaxLength(128).IsRequired();
            entity.Property(entry => entry.Operation).HasMaxLength(512).IsRequired();
            entity.Property(entry => entry.Destination).HasMaxLength(1024);
            entity.Property(entry => entry.PayloadType).HasMaxLength(1024).IsRequired();
            entity.Property(entry => entry.Payload).IsRequired();
            entity.Property(entry => entry.ContentType).HasMaxLength(256).IsRequired();
            entity.Property(entry => entry.CorrelationId).HasMaxLength(256);
            entity.Property(entry => entry.TraceId).HasMaxLength(256);
            entity.Property(entry => entry.Status).HasMaxLength(64).IsRequired();
            entity.Property(entry => entry.HeadersJson).HasColumnType("nvarchar(max)");
            entity.Property(entry => entry.MetadataJson).HasColumnType("nvarchar(max)");
            entity.Property(entry => entry.FailureJson).HasColumnType("nvarchar(max)");
        });

        return modelBuilder;
    }

    /// <summary>
    /// Adds both the KnOwl inbox and outbox tables and indexes to the model when configuring a model manually.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="schema">The optional table schema.</param>
    /// <returns>The same model builder for fluent configuration.</returns>
    public static ModelBuilder ApplyKnOwl(
        this ModelBuilder modelBuilder,
        string schema = null)
        => modelBuilder.ApplyKnOwlInbox(schema: schema).ApplyKnOwlOutbox(schema: schema);
}
