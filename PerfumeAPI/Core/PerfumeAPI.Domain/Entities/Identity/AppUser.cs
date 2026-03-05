using Microsoft.AspNetCore.Identity;

namespace PerfumeAPI.Domain.Entities.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    
    // Storing the calculated 100-dimensional user preference vector as a JSON string
    public string? UserVector { get; set; }
}
