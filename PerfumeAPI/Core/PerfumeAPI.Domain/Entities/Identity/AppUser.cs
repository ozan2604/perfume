using Microsoft.AspNetCore.Identity;

namespace PerfumeAPI.Domain.Entities.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Gender { get; set; }
    public DateTime? BirthDate { get; set; }
}
