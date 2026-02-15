using Depreeeemmmm.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Depreeeemmmm.Data.Mappings;

public class AdminLocationMap : IEntityTypeConfiguration<AdminLocation>
{
    public void Configure(EntityTypeBuilder<AdminLocation> builder)
    {
        builder.ToTable("AdminLocations");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .HasColumnType("nvarchar(100)")
            .IsRequired();
        
        builder.Property(x => x.Latitude)
            .HasColumnType("float")
            .IsRequired();

        builder.Property(x => x.longitude)
            .HasColumnType("float")
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