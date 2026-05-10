using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeAPI.Domain.Entities;
using PerfumeAPI.Persistence.Contexts;
using PerfumeAPI.WebApi.Services;

namespace PerfumeAPI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerfumesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RecommendationService _recommendationService;

    public PerfumesController(AppDbContext context, RecommendationService recommendationService)
    {
        _context = context;
        _recommendationService = recommendationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPerfumes(
        int page = 1, 
        int pageSize = 20,
        string? gender = null,
        string? country = null,
        string? sortBy = null,
        string? search = null)
    {
        // 1. AsNoTracking() ile EF Core'un veriyi hafızada takip etmesini engelliyoruz (Sade Okuma hızlanır)
        // 2. Sadece arayüzde (UI) gereken ana bilgileri "Select" yardımıyla çekiyoruz.
        // BÖYLECE 256 boyutlu devasa "EmbeddingVector" gibi kolonlar veritabanından hiç gönderilmiyor! (10 KAT HIZLANMA)
        
        var query = _context.Perfumes.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchLower) || 
                (p.Brand != null && p.Brand.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrEmpty(gender))
        {
            query = query.Where(p => p.Gender == gender);
        }

        if (!string.IsNullOrEmpty(country))
        {
            query = query.Where(p => p.Country == country);
        }

        if (sortBy == "rating_desc")
        {
            query = query.OrderByDescending(p => p.RatingValue);
        }
        else if (sortBy == "price_asc")
        {
            query = query.OrderBy(p => p.Price);
        }
        else if (sortBy == "price_desc")
        {
            query = query.OrderByDescending(p => p.Price);
        }
        else if (sortBy == "a-z")
        {
            query = query.OrderBy(p => p.Name);
        }
        else
        {
            query = query.OrderBy(p => p.Name);
        }

         var perfumes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new 
            {
                p.Id,
                p.Name,
                p.Brand,
                p.Url,
                p.ImageUrl,
                p.Gender,
                p.Country,
                Rating = p.RatingValue,
                p.RatingCount,
                p.MainAccord1,
                p.MainAccord2,
                p.MainAccord3,
                p.TopNotes,
                p.Price
            })
            .ToListAsync();

        return Ok(perfumes);
    }

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries()
    {
        var countries = await _context.Perfumes
            .AsNoTracking()
            .Where(p => !string.IsNullOrEmpty(p.Country))
            .Select(p => p.Country)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
            
        return Ok(countries);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPerfume(Guid id)
    {
        var perfume = await _context.Perfumes.FindAsync(id);
        if (perfume == null) return NotFound();
        return Ok(perfume);
    }

    [HttpPost("interact")]
    [Authorize]
    public async Task<IActionResult> Interact([FromBody] InteractionRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var user = await _context.Users.FindAsync(userId);
        var perfume = await _context.Perfumes.FindAsync(request.PerfumeId);

        var existing = await _context.UserPerfumeInteractions
            .FirstOrDefaultAsync(i => i.UserId == userId && i.PerfumeId == request.PerfumeId);

        if (existing != null)
        {
            existing.IsLiked = request.IsLiked;
            existing.InteractionDate = DateTime.UtcNow;
            
            // Güncellenen kolonlar
            existing.UserName = user?.Name;
            existing.UserSurname = user?.Surname;
            existing.UserEmail = user?.Email;
            existing.PerfumeName = perfume?.Name;
        }
        else
        {
            _context.UserPerfumeInteractions.Add(new UserPerfumeInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PerfumeId = request.PerfumeId,
                IsLiked = request.IsLiked,
                InteractionDate = DateTime.UtcNow,
                UserName = user?.Name,
                UserSurname = user?.Surname,
                UserEmail = user?.Email,
                PerfumeName = perfume?.Name
            });
        }

        await _context.SaveChangesAsync();
        
        // Trigger background user vector update
        await _recommendationService.UpdateUserVectorAsync(userId);

        return Ok(new { Message = "Interaction recorded and profile updated." });
    }

    [HttpPost("interact-bulk")]
    [Authorize]
    public async Task<IActionResult> InteractBulk([FromBody] List<InteractionRequest> requests)
    {
        if (requests == null || !requests.Any())
            return BadRequest("No interactions provided.");

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        
        var userId = Guid.Parse(userIdString);
        var user = await _context.Users.FindAsync(userId);
        
        var perfumeIds = requests.Select(r => r.PerfumeId).ToList();
        
        var perfumes = await _context.Perfumes
            .Where(p => perfumeIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        
        var existingInteractions = await _context.UserPerfumeInteractions
            .Where(i => i.UserId == userId && perfumeIds.Contains(i.PerfumeId))
            .ToListAsync();

        foreach (var request in requests)
        {
            var perfumeName = perfumes.TryGetValue(request.PerfumeId, out var pf) ? pf.Name : null;
            var existing = existingInteractions.FirstOrDefault(i => i.PerfumeId == request.PerfumeId);
            
            if (existing != null)
            {
                existing.IsLiked = request.IsLiked;
                existing.InteractionDate = DateTime.UtcNow;
                
                existing.UserName = user?.Name;
                existing.UserSurname = user?.Surname;
                existing.UserEmail = user?.Email;
                existing.PerfumeName = perfumeName;
            }
            else
            {
                _context.UserPerfumeInteractions.Add(new UserPerfumeInteraction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    PerfumeId = request.PerfumeId,
                    IsLiked = request.IsLiked,
                    InteractionDate = DateTime.UtcNow,
                    UserName = user?.Name,
                    UserSurname = user?.Surname,
                    UserEmail = user?.Email,
                    PerfumeName = perfumeName
                });
            }
        }

        await _context.SaveChangesAsync();
        
        // Trigger background user vector update
        await _recommendationService.UpdateUserVectorAsync(userId);

        return Ok(new { Message = $"{requests.Count} interactions recorded and profile updated." });
    }

    [HttpGet("interactions")]
    [Authorize]
    public async Task<IActionResult> GetInteractions()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var interactions = await _context.UserPerfumeInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => new { perfumeId = i.PerfumeId.ToString(), isLiked = i.IsLiked })
            .ToListAsync();

        // Return as dictionary: { "perfumeId": true/false }
        var dict = interactions.ToDictionary(i => i.perfumeId, i => i.isLiked);
        return Ok(dict);
    }

    [HttpGet("my-interactions")]
    [Authorize]
    public async Task<IActionResult> GetMyInteractions()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        
        var interactions = await _context.UserPerfumeInteractions
            .AsNoTracking()
            .Include(i => i.Perfume)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.InteractionDate)
            .Select(i => new 
            {
                Perfume = new 
                {
                    i.Perfume.Id,
                    i.Perfume.Name,
                    i.Perfume.Brand,
                    i.Perfume.Url,
                    i.Perfume.ImageUrl,
                    i.Perfume.Gender,
                    Rating = i.Perfume.RatingValue,
                    i.Perfume.RatingCount,
                    i.Perfume.MainAccord1,
                    i.Perfume.MainAccord2,
                    i.Perfume.MainAccord3,
                    i.Perfume.TopNotes,
                    i.Perfume.Price
                },
                IsLiked = i.IsLiked,
                InteractionDate = i.InteractionDate
            })
            .ToListAsync();

        return Ok(interactions);
    }

    [HttpGet("interactions/count")]
    [Authorize]
    public async Task<IActionResult> GetInteractionCount()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var count = await _context.UserPerfumeInteractions.AsNoTracking().CountAsync(i => i.UserId == userId);

        return Ok(new { Count = count });
    }

    [HttpGet("profile-summary")]
    [Authorize]
    public async Task<IActionResult> GetProfileSummary()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var summary = await _recommendationService.GenerateUserProfileSummaryAsync(userId);
        
        return Ok(new { Summary = summary });
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(int count = 25)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var recommendations = await _recommendationService.GetRecommendationsAsync(userId, count);

        var result = recommendations.Select(r => new 
        {
            id = r.Perfume.Id,
            name = r.Perfume.Name,
            brand = r.Perfume.Brand,
            gender = r.Perfume.Gender,
            country = r.Perfume.Country,
            rating = r.Perfume.RatingValue,
            ratingCount = r.Perfume.RatingCount,
            year = r.Perfume.Year,
            mainAccord1 = r.Perfume.MainAccord1,
            mainAccord2 = r.Perfume.MainAccord2,
            mainAccord3 = r.Perfume.MainAccord3,
            url = r.Perfume.Url,
            imageUrl = r.Perfume.ImageUrl,
            price = r.Perfume.Price,
            explanation = r.Explanation
        });

        return Ok(result);
    }
}

public class InteractionRequest
{
    public Guid PerfumeId { get; set; }
    public bool IsLiked { get; set; }
}
