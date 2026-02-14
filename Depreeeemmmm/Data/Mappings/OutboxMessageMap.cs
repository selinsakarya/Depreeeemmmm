using Depreeeemmmm.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depreeeemmmm.Data.Mappings;

public class OutboxMessageMap : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Data)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnType("datetime(2,7)")
            .IsRequired();
        
        builder.Property(x => x.Status)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(x => x.ProcessedDate)
            .HasColumnType("datetime(2,7)");
        
        builder.Property(x => x.Type)
            .HasColumnType("varchar(256)")
            .IsRequired();

        builder.Property(x => x.QueueName)
            .HasColumnType("varchar(256)");
        
        builder.Property(x => x.RoutingKey)
            .HasColumnType("varchar(256)");
    }
}