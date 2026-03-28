using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using PerfumeAPI.Domain.Entities.Identity;

namespace PerfumeAPI.Domain.Entities
{
    public class UserPerfumeInteraction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } = null!;

     
        [Required]
        public Guid PerfumeId { get; set; }

        [ForeignKey("PerfumeId")]
        public virtual Perfume Perfume { get; set; } = null!;

        // Denormalize edilmiş, tablo üzerinde doğrudan görünmek istenen alanlar
        public string? UserName { get; set; }
        public string? UserSurname { get; set; }
        public string? UserEmail { get; set; }
        public string? PerfumeName { get; set; }

        // Kullanıcının tercihi: true = Like, false = Dislike
        [Required]
        public bool IsLiked { get; set; }

        // Etkileşim zamanı (Vektör güncelliği ve analiz için)
        public DateTime InteractionDate { get; set; } = DateTime.UtcNow;
    }
}
