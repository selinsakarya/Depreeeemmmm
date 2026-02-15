using Depreeeemmmm.Data.Enums;
using Microsoft.SqlServer.Types;
using NetTopologySuite.Geometries;

namespace Depreeeemmmm.Data.Entities;

public class Earthquake
{
    public int Id { get; set; }
    
    public Guid SecondaryUniqueId { get; set; }
    
    public Point Coordinates { get; set; } 

    public double Magnitude { get; set; }

    public double Depth { get; set; }

    public DateTime OccurredAt { get; set; }

    public string IntegrationReferenceId { get; set; }
    
    public EarthquakeSource Source { get; set; }

    public string Location { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}