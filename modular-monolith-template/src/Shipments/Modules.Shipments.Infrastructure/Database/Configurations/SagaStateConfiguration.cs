using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Common.Domain.Saga;

namespace Modules.Shipments.Infrastructure.Database.Configurations;

internal sealed class SagaStateConfiguration : IEntityTypeConfiguration<SagaState>
{
    public void Configure(EntityTypeBuilder<SagaState> builder)
    {
        builder.ToTable("SagaStates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SagaType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CurrentStep)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.CompensationData);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.CorrelationId)
            .IsUnique()
            .HasDatabaseName("IX_SagaStates_CorrelationId");

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_SagaStates_Status_CreatedAt");
    }
}
