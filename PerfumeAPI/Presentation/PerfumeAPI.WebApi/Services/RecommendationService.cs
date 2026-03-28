using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PerfumeAPI.Domain.Entities;
using PerfumeAPI.Domain.Entities.Identity;
using PerfumeAPI.Persistence.Contexts;

namespace PerfumeAPI.WebApi.Services;

using Microsoft.Extensions.Caching.Memory;

public class RecommendationService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private const string VectorsCacheKey = "PerfumeVectors";

    public RecommendationService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // JSON Parse işlemi C#'ta çok ağırdır (24.000 veri için saniyeler alır).
    // Bu metod tüm vektörleri 1 kere çekip RAM'e atar. Sonraki tüm kullanıcılarda 1 milisaniyede gelir.
    private async Task<Dictionary<Guid, float[]>> GetAllPerfumeVectorsAsync()
    {
        if (!_cache.TryGetValue(VectorsCacheKey, out Dictionary<Guid, float[]>? vectors))
        {
            var dbPerfumes = await _context.Perfumes
                .AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.EmbeddingVector))
                .Select(p => new { p.Id, p.EmbeddingVector }) // Devasa satırları çekmekten kurtarır
                .ToListAsync();

            vectors = dbPerfumes.ToDictionary(
                p => p.Id,
                p => JsonSerializer.Deserialize<float[]>(p.EmbeddingVector!)!
            );

            // 24 saat RAM'de tut
            _cache.Set(VectorsCacheKey, vectors, TimeSpan.FromHours(24)); 
        }
        return vectors!;
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

    public async Task<List<Perfume>> GetRecommendationsAsync(Guid userId, int count = 10)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.UserVector))
            return await _context.Perfumes.AsNoTracking().OrderBy(p => Guid.NewGuid()).Take(count).ToListAsync(); 

        var userVec = JsonSerializer.Deserialize<float[]>(user.UserVector!)!;
        
        // RAM'den 24.000 vektörü sadece 1 milisaniyede getirir (JSON Parsinge girmeden)
        var allVectors = await GetAllPerfumeVectorsAsync();

        // 1. Etkileşime girilmiş ID'leri alıp Set'e koyuyoruz (O(1) arama hızı için)
        var interactedIds = await _context.UserPerfumeInteractions
            .Where(i => i.UserId == userId)
            .Select(i => i.PerfumeId)
            .ToListAsync();
        var interactedSet = new HashSet<Guid>(interactedIds);

        // 2. Hafızadaki vektörler arasında Cosine Similarity hesapla (Çok hızlıdır)
        var topRecommendedIds = allVectors
            .Where(kvp => !interactedSet.Contains(kvp.Key))
            .Select(kvp => new 
            {
                Id = kvp.Key,
                Similarity = CosineSimilarity(userVec, kvp.Value)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(count)
            .Select(x => x.Id)
            .ToList();

        // 3. Sadece en çok benzeyen 10 parfümün "Tüm Bilgilerini" Veritabanından çek. (24.000 çekmekten kurtulduk!)
        var recommendedPerfumes = await _context.Perfumes
            .AsNoTracking()
            .Where(p => topRecommendedIds.Contains(p.Id))
            .ToListAsync();

        // 4. Veritabanından gelen veriyi Similarity sırasına göre tekrar dizip geri dön
        return topRecommendedIds
            .Select(id => recommendedPerfumes.First(p => p.Id == id))
            .ToList();
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
