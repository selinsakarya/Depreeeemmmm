namespace Depreeeemmmm.Data.Entities;

public class AdminLocation
{
    public int Id { get; set; }

    public string Name { get; set; }

    public double Latitude { get; set; }
    
    public double longitude { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}