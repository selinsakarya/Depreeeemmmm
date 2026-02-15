namespace Depreeeemmmm.Data.Entities;

public class Configuration
{
    public int Id { get; set; }

    public string Key { get; set; }

    public string Value { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}