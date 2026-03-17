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
    public async Task<IActionResult> GetPerfumes(int page = 1, int pageSize = 20)
    {
        var perfumes = await _context.Perfumes
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(perfumes);
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
        
        var existing = await _context.UserPerfumeInteractions
            .FirstOrDefaultAsync(i => i.UserId == userId && i.PerfumeId == request.PerfumeId);

        if (existing != null)
        {
            existing.IsLiked = request.IsLiked;
            existing.InteractionDate = DateTime.UtcNow;
        }
        else
        {
            _context.UserPerfumeInteractions.Add(new UserPerfumeInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PerfumeId = request.PerfumeId,
                IsLiked = request.IsLiked,
                InteractionDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        
        // Trigger background user vector update
        await _recommendationService.UpdateUserVectorAsync(userId);

        return Ok(new { Message = "Interaction recorded and profile updated." });
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(int count = 10)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var recommendations = await _recommendationService.GetRecommendationsAsync(userId, count);

        return Ok(recommendations);
    }
}

public class InteractionRequest
{
    public Guid PerfumeId { get; set; }
    public bool IsLiked { get; set; }
}
