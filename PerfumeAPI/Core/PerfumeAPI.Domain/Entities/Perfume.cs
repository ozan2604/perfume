namespace PerfumeAPI.Domain.Entities;

public class Perfume
{
    public Guid Id { get; set; }
    public string? Url { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Brand { get; set; }
    public string? Country { get; set; }
    public string? Gender { get; set; }
    public double? RatingValue { get; set; }
    public int? RatingCount { get; set; }
    public int? Year { get; set; }
    public decimal? Price { get; set; }
    
    public string? TopNotes { get; set; }
    public string? MiddleNotes { get; set; }
    public string? BaseNotes { get; set; }
    
    // Original textual accords
    public string? MainAccord1 { get; set; }
    public string? MainAccord2 { get; set; }
    public string? MainAccord3 { get; set; }
    public string? MainAccord4 { get; set; }
    public string? MainAccord5 { get; set; }

    public string? Perfumer1 { get; set; }
    public string? Perfumer2 { get; set; }
    public string? Perfumer3 { get; set; }
    public string? Perfumer4 { get; set; }

    // Storing the calculated 100-dimensional vector as a JSON string
    public string? EmbeddingVector { get; set; }
}
