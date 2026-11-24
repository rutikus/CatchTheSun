namespace CatchTheSun.Core.Entities;

public class Site
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    public ICollection<Calculation> Calculations { get; set; } = new List<Calculation>();
}
