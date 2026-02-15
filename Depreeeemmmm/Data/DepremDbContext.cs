using Depreeeemmmm.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Depreeeemmmm.Data;

public class DepremDbContext : DbContext
{
    public DepremDbContext(DbContextOptions<DepremDbContext> options) : base(options)
    {
        
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyConfigurationsFromAssembly(typeof(DepremDbContext).Assembly);
    }

    public DbSet<Earthquake> Earthquakes { get; set; }
    
    public DbSet<AdminLocation> AdminLocations { get; set; }
    
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
}