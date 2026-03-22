using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PerfumeAPI.Domain.Entities;
using PerfumeAPI.Domain.Entities.Identity;
using PerfumeAPI.Persistence.Contexts;

namespace PerfumeAPI.WebApi.Services;

public class RecommendationService
{
    private readonly AppDbContext _context;

    public RecommendationService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Calculates the user's preference vector based on liked and disliked perfumes.
    /// Logic: Average(LikedVectors) - 0.5 * Average(DislikedVectors)
    /// </summary>
    public async Task UpdateUserVectorAsync(Guid userId)
    {
        var interactions = await _context.UserPerfumeInteractions
            .Where(i => i.UserId == userId)
            .Include(i => i.Perfume)
            .ToListAsync();

        if (!interactions.Any()) return;

        var likedVectors = interactions
            .Where(i => i.IsLiked && !string.IsNullOrEmpty(i.Perfume.EmbeddingVector))
            .Select(i => JsonSerializer.Deserialize<float[]>(i.Perfume.EmbeddingVector!)!)
            .ToList();

        var dislikedVectors = interactions
            .Where(i => !i.IsLiked && !string.IsNullOrEmpty(i.Perfume.EmbeddingVector))
            .Select(i => JsonSerializer.Deserialize<float[]>(i.Perfume.EmbeddingVector!)!)
            .ToList();

        if (!likedVectors.Any() && !dislikedVectors.Any()) return;

        float[] resultVector = new float[256]; // Assuming 256 dimensions from Python SVD

        if (likedVectors.Any())
        {
            float[] avgLiked = CalculateAverage(likedVectors);
            for (int i = 0; i < 256; i++) resultVector[i] += avgLiked[i];
        }

        if (dislikedVectors.Any())
        {
            float[] avgDisliked = CalculateAverage(dislikedVectors);
            for (int i = 0; i < 256; i++) resultVector[i] -= avgDisliked[i] * 0.5f; // Penalty weight
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.UserVector = JsonSerializer.Serialize(resultVector);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Finds perfumes most similar to the user's preference vector.
    /// </summary>
    public async Task<List<Perfume>> GetRecommendationsAsync(Guid userId, int count = 10)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.UserVector))
            return await _context.Perfumes.OrderBy(p => Guid.NewGuid()).Take(count).ToListAsync(); // Random if no vector

        var userVec = JsonSerializer.Deserialize<float[]>(user.UserVector!)!;

        // Get all perfumes with vectors (In a real app, you'd cache these or use a vector DB)
        var perfumes = await _context.Perfumes
            .Where(p => !string.IsNullOrEmpty(p.EmbeddingVector))
            .ToListAsync();

        // Already interacted items shouldn't be recommended
        var interactedIds = await _context.UserPerfumeInteractions
            .Where(i => i.UserId == userId)
            .Select(i => i.PerfumeId)
            .ToListAsync();

        var recommended = perfumes
            .Where(p => !interactedIds.Contains(p.Id))
            .Select(p => new 
            {
                Perfume = p,
                Similarity = CosineSimilarity(userVec, JsonSerializer.Deserialize<float[]>(p.EmbeddingVector!)!)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(count)
            .Select(x => x.Perfume)
            .ToList();

        return recommended;
    }

    private float[] CalculateAverage(List<float[]> vectors)
    {
        int dims = vectors[0].Length;
        float[] avg = new float[dims];
        foreach (var vec in vectors)
        {
            for (int i = 0; i < dims; i++) avg[i] += vec[i];
        }
        for (int i = 0; i < dims; i++) avg[i] /= vectors.Count;
        return avg;
    }

    private float CosineSimilarity(float[] vecA, float[] vecB)
    {
        float dotProduct = 0;
        float magA = 0;
        float magB = 0;
        for (int i = 0; i < vecA.Length; i++)
        {
            dotProduct += vecA[i] * vecB[i];
            magA += vecA[i] * vecA[i];
            magB += vecB[i] * vecB[i];
        }
        float mag = (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
        return mag == 0 ? 0 : dotProduct / mag;
    }
}
