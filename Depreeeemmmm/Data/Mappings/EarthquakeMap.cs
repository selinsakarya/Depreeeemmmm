using System.Data.SqlTypes;
using Depreeeemmmm.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.SqlServer.Types;

namespace Depreeeemmmm.Data.Mappings;

public class EarthquakeMap : IEntityTypeConfiguration<Earthquake>
{
    public void Configure(EntityTypeBuilder<Earthquake> builder)
    {
        builder.ToTable("Earthquakes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SecondaryUniqueId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        
        builder.Property(x => x.Magnitude)
            .HasColumnType("float")
            .IsRequired();
        
        builder.Property(x => x.Depth)
            .HasColumnType("float")
            .IsRequired();

        builder.Property(x => x.Coordinates)
            .HasColumnType("geography")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnType("datetime2(7)")
            .IsRequired();

        builder.Property(x => x.IntegrationReferenceId)
            .HasColumnType("varchar(150)")
            .IsRequired();

        builder.Property(x => x.Source)
            .HasColumnType("int")
            .IsRequired();
        
        builder.Property(x => x.Location)
            .HasColumnType("nvarchar(300)")
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