using Depreeeemmmm.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depreeeemmmm.Data.Mappings;

public class ConfigurationMap : IEntityTypeConfiguration<Configuration>
{
    public void Configure(EntityTypeBuilder<Configuration> builder)
    {
        builder.ToTable("Configurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasColumnType("varchar(100)")
            .IsRequired();
        
        builder.Property(x => x.Value)
            .HasColumnType("nvarchar(250)")
            .IsRequired();
        
        builder.Property(x => x.IsDeleted)
            .HasColumnType("bit")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(7)")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnType("nvarchar(100)")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(7)")
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasColumnType("nvarchar(100)")
            .IsRequired();

        builder.HasQueryFilter(x => x.IsDeleted == false);

    }
}