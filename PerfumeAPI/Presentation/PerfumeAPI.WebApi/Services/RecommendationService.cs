using System.Text.Json;
using System.Numerics;
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

    public async Task WarmupCacheAsync()
    {
        await GetAllPerfumeVectorsAsync();
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

            // Parallelize the heavy JSON deserialization
            vectors = dbPerfumes
                .AsParallel()
                .ToDictionary(
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
    /// Logic: Average(LikedVectors) - 0.1 * Average(DislikedVectors)
    /// </summary>
    public async Task UpdateUserVectorAsync(Guid userId)
    {
        var interactions = await _context.UserPerfumeInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => new { i.PerfumeId, i.IsLiked })
            .ToListAsync();

        if (!interactions.Any()) return;

        var allVectors = await GetAllPerfumeVectorsAsync();

        var likedVectors = interactions
            .Where(i => i.IsLiked && allVectors.ContainsKey(i.PerfumeId))
            .Select(i => allVectors[i.PerfumeId])
            .ToList();

        var dislikedVectors = interactions
            .Where(i => !i.IsLiked && allVectors.ContainsKey(i.PerfumeId))
            .Select(i => allVectors[i.PerfumeId])
            .ToList();

        if (!likedVectors.Any() && !dislikedVectors.Any()) return;

        float[] resultVector = new float[320]; // Assuming 320 dimensions from Python SVD

        if (likedVectors.Any())
        {
            float[] avgLiked = CalculateAverage(likedVectors);
            for (int i = 0; i < 320; i++) resultVector[i] += avgLiked[i];
        }

        if (dislikedVectors.Any())
        {
            float[] avgDisliked = CalculateAverage(dislikedVectors);
            for (int i = 0; i < 320; i++) resultVector[i] -= avgDisliked[i] * 0.1f; // Penalty weight
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.UserVector = JsonSerializer.Serialize(resultVector);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<RecommendationDto>> GetRecommendationsAsync(Guid userId, int count = 10)
    {
        var interactionsRaw = await _context.UserPerfumeInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => new 
            { 
                i.PerfumeId, 
                i.IsLiked,
                i.Perfume.Name,
                i.Perfume.MainAccord1,
                i.Perfume.MainAccord2,
                i.Perfume.MainAccord3
            })
            .ToListAsync();

        var interactions = interactionsRaw.Select(i => new
        {
            i.PerfumeId,
            i.IsLiked,
            Perfume = new Perfume
            {
                Id = i.PerfumeId,
                Name = i.Name,
                MainAccord1 = i.MainAccord1,
                MainAccord2 = i.MainAccord2,
                MainAccord3 = i.MainAccord3
            }
        }).ToList();

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        
        // Eğer 10 etkileşimden azsa veya profil vektörü henüz oluşmadıysa boş dön
        if (user == null || string.IsNullOrEmpty(user.UserVector) || interactions.Count < 10)
        {
            return new List<RecommendationDto>();
        }

        var userVec = JsonSerializer.Deserialize<float[]>(user.UserVector!)!;
        
        var allVectors = await GetAllPerfumeVectorsAsync();

        var interactedIds = interactions.Select(i => i.PerfumeId).ToList();
        var interactedSet = new HashSet<Guid>(interactedIds);

        var likedPerfumes = interactions.Where(i => i.IsLiked).Select(i => i.Perfume).ToList();

        var topRecommendedIds = allVectors
            .AsParallel()
            .Where(kvp => !interactedSet.Contains(kvp.Key))
            .Select(kvp => new 
            {
                Id = kvp.Key,
                Similarity = CosineSimilarity(userVec, kvp.Value)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(count)
            .ToList();

        var recommendedIdsList = topRecommendedIds.Select(x => x.Id).ToList();

        var recommendedPerfumes = await _context.Perfumes
            .AsNoTracking()
            .Where(p => recommendedIdsList.Contains(p.Id))
            .Select(p => new Perfume
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                Url = p.Url,
                ImageUrl = p.ImageUrl,
                Gender = p.Gender,
                Country = p.Country,
                RatingValue = p.RatingValue,
                RatingCount = p.RatingCount,
                MainAccord1 = p.MainAccord1,
                MainAccord2 = p.MainAccord2,
                MainAccord3 = p.MainAccord3,
                TopNotes = p.TopNotes,
                Price = p.Price
            })
            .ToListAsync();

        var result = new List<RecommendationDto>();

        foreach (var rec in topRecommendedIds)
        {
            var p = recommendedPerfumes.First(x => x.Id == rec.Id);
            string explanation = GenerateExplanation(p, likedPerfumes, allVectors);
            
            result.Add(new RecommendationDto 
            { 
                Perfume = p, 
                Explanation = explanation 
            });
        }

        return result;
    }

    private string GenerateExplanation(Perfume recommended, List<Perfume> likedPerfumes, Dictionary<Guid, float[]> allVectors)
    {
        if (!likedPerfumes.Any()) return "Genel profilinize dayanarak özenle seçildi.";

        if (allVectors.TryGetValue(recommended.Id, out var recVec))
        {
            var bestMatch = likedPerfumes
                .Where(lp => allVectors.ContainsKey(lp.Id))
                .Select(lp => new { Perfume = lp, Sim = CosineSimilarity(recVec, allVectors[lp.Id]) })
                .OrderByDescending(x => x.Sim)
                .FirstOrDefault();

            if (bestMatch != null && bestMatch.Sim > 0.65f) // Threshold for item-based similarity
            {
                var recAccordsList = new[] { recommended.MainAccord1, recommended.MainAccord2, recommended.MainAccord3 }.Where(a => !string.IsNullOrEmpty(a)).ToList();
                var matchAccords = new[] { bestMatch.Perfume.MainAccord1, bestMatch.Perfume.MainAccord2, bestMatch.Perfume.MainAccord3 }.Where(a => !string.IsNullOrEmpty(a)).ToList();
                var sharedCount = recAccordsList.Intersect(matchAccords).ToList();

                var formattedName = FormatName(bestMatch.Perfume.Name);

                if (sharedCount.Any())
                {
                    return $"Beğendiğin '{formattedName}' parfümüne çok benziyor; özellikle {string.Join(" ve ", sharedCount.Take(2))} dokuları bu parfümde de hakim.";
                }

                return $"Beğendiğin '{formattedName}' parfümüne çok benziyor.";
            }
        }

        // Fallback: Content-based (Notes / Accords logic)
        var likedAccords = likedPerfumes
            .SelectMany(lp => new[] { lp.MainAccord1, lp.MainAccord2, lp.MainAccord3 })
            .Where(a => !string.IsNullOrEmpty(a))
            .GroupBy(a => a)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(4)
            .ToList();

        var recAccords = new[] { recommended.MainAccord1, recommended.MainAccord2, recommended.MainAccord3 }
            .Where(a => !string.IsNullOrEmpty(a))
            .ToList();

        var sharedAccord = recAccords.FirstOrDefault(a => likedAccords.Contains(a));

        if (sharedAccord != null)
        {
            return $"Sevdiğin '{sharedAccord}' ağırlıklı notaları içeriyor.";
        }

        return "Parfüm profilinizle yüksek oranda eşleşiyor.";
    }

    private string FormatName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var words = name.Replace("-", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return string.Join(" ", words);
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
        if (vecA.Length != vecB.Length) return 0;
        
        int length = vecA.Length;
        int vectorSize = Vector<float>.Count;
        int i = 0;

        Vector<float> vDot = Vector<float>.Zero;
        Vector<float> vMagA = Vector<float>.Zero;
        Vector<float> vMagB = Vector<float>.Zero;

        while (i <= length - vectorSize)
        {
            var va = new Vector<float>(vecA, i);
            var vb = new Vector<float>(vecB, i);
            vDot += va * vb;
            vMagA += va * va;
            vMagB += vb * vb;
            i += vectorSize;
        }

        float dotProduct = Vector.Dot(vDot, Vector<float>.One);
        float magA = Vector.Dot(vMagA, Vector<float>.One);
        float magB = Vector.Dot(vMagB, Vector<float>.One);

        for (; i < length; i++)
        {
            dotProduct += vecA[i] * vecB[i];
            magA += vecA[i] * vecA[i];
            magB += vecB[i] * vecB[i];
        }

        float mag = (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
        return mag == 0 ? 0 : dotProduct / mag;
    }

    public async Task<string> GenerateUserProfileSummaryAsync(Guid userId)
    {
        var interactionsRaw = await _context.UserPerfumeInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => new 
            {
                i.IsLiked,
                i.Perfume.Id,
                i.Perfume.RatingValue,
                i.Perfume.Year,
                i.Perfume.Gender,
                i.Perfume.Country,
                i.Perfume.MainAccord1,
                i.Perfume.MainAccord2,
                i.Perfume.MainAccord3
            })
            .ToListAsync();

        if (interactionsRaw.Count < 10) 
            return $"Koku profilinin haritasını tam çıkarabilmemiz için en az 10 parfüm değerlendirmelisin. Keşfetmeye devam et! (Şu anki: {interactionsRaw.Count}/10)";

        var liked = interactionsRaw.Where(i => i.IsLiked).Select(i => new Perfume 
        { 
            Id = i.Id, 
            RatingValue = i.RatingValue, 
            Year = i.Year, 
            Gender = i.Gender, 
            Country = i.Country, 
            MainAccord1 = i.MainAccord1, 
            MainAccord2 = i.MainAccord2, 
            MainAccord3 = i.MainAccord3 
        }).ToList();
        
        var disliked = interactionsRaw.Where(i => !i.IsLiked).Select(i => new Perfume 
        { 
            Id = i.Id, 
            MainAccord1 = i.MainAccord1, 
            MainAccord2 = i.MainAccord2, 
            MainAccord3 = i.MainAccord3 
        }).ToList();

        if (!liked.Any())
            return "Şu ana kadar tarzını yansıtacak o 'imza' kokuları bulamamışız gibi görünüyor. Belki biraz daha cesur notalar denemelisin.";

        var summary = new System.Text.StringBuilder();

        // 1. Rating Preference (Ekspertiz seviyesi)
        var avgLikedRating = liked.Where(p => p.RatingValue.HasValue).Average(p => p.RatingValue);
        if (avgLikedRating >= 4.10)
            summary.Append($"Koku profilin oldukça rafine; kitlelerin de kalitesini onayladığı (ortalama {avgLikedRating:F2} puanlı) başyapıt niteliğindeki elit tasarımlara çekiliyorsun. ");
        else if (avgLikedRating >= 3.8)
            summary.Append($"Kalite standartların oturmuş durumda (ortalama {avgLikedRating:F2} puan). Hem bilindik hem de karakterli harmanları dengede tutuyorsun. ");
        else if (avgLikedRating > 0)
            summary.Append($"Popüler ve risksiz kokular yerine, daha niş, çok bilinmeyen ve puan ortalamalarının dışına çıkan aykırı tasarımları ({avgLikedRating:F2}) seviyorsun. ");

        // 2. Era / Yıl Tercihi
        var years = liked.Where(p => p.Year.HasValue).Select(p => p.Year.Value).ToList();
        if (years.Any())
        {
            var avgYear = years.Average();
            if (avgYear < 2000)
                summary.Append($"Parfüm dünyasının {Math.Round(avgYear / 10) * 10}'li yıllarına damga vuran retro, karakterli ve klasikleşmiş şaheserler tam sana göre. ");
            else if (avgYear > 2018)
                summary.Append("Günümüz modern parfüm trendlerini yakından takip ediyor, daha şeffaf ve yeni nesil deneysel harmanları tercih ediyorsun. ");
        }

        // 3. Ülke/Coğrafya Cinsiyet Tercihi
        var genders = liked.Where(p => !string.IsNullOrEmpty(p.Gender)).GroupBy(p => p.Gender.ToLower()).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
        if (genders.FirstOrDefault() == "unisex")
            summary.Append("Keskin cinsiyet kalıplarını reddediyor, zıtlıkların uyumunu barındıran 'unisex' harmanların sunduğu o özgürlükçü havayı benimsiyorsun. ");

        var topCountries = liked.Where(p => !string.IsNullOrEmpty(p.Country))
                                .GroupBy(p => p.Country)
                                .OrderByDescending(g => g.Count())
                                .Select(g => g.Key)
                                .Take(2)
                                .ToList();

        if (topCountries.Any())
        {
            var countryNotes = new List<string>();
            foreach(var c in topCountries)
            {
                var lowerC = c.ToLower();
                if (lowerC.Contains("france") || lowerC.Contains("fransa")) countryNotes.Add("Fransız ekolünün zarif, köklü ve aristokrat aurasını");
                else if (lowerC.Contains("italy") || lowerC.Contains("italya")) countryNotes.Add("İtalyan parfümörlerin Akdeniz esintili, canlı ve neşeli karakterini");
                else if (lowerC.Contains("oman") || lowerC.Contains("uae") || lowerC.Contains("saudi") || lowerC.Contains("arab")) countryNotes.Add("Ortadoğu'nun egzotik, gizemli ve ihtişamlı baharat diyarlarını");
                else if (lowerC.Contains("usa") || lowerC.Contains("united states")) countryNotes.Add("Amerikan tarzı rahat, yenilikçi ve dinamik koku konseptlerini");
                else if (lowerC.Contains("uk") || lowerC.Contains("united kingdom")) countryNotes.Add("İngiliz asaletini yansıtan klasik barbershop dokunuşlarını");
                else if (lowerC.Contains("spain") || lowerC.Contains("ispanya")) countryNotes.Add("İspanyol sıcaklığını barındıran tutkulu esansları");
                else countryNotes.Add($"{c} coğrafyasının kendine has, imza niteliğindeki koku genetiğini");
            }
            summary.Append($"Coğrafi kökler olarak özellikle {string.Join(" ve ", countryNotes)} ruhuna çok daha yakın hissediyorsun. ");
        }

        // 4. Sevilen Notalar
        var likedAccords = liked
            .SelectMany(p => new[] { p.MainAccord1, p.MainAccord2, p.MainAccord3 })
            .Where(a => !string.IsNullOrEmpty(a))
            .GroupBy(a => a)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Distinct()
            .Take(2)
            .ToList();

        if (likedAccords.Any())
        {
            summary.Append($"Özellikle içindeki o tutkuyu açığa çıkaran '{string.Join("' ve '", likedAccords)}' akorları imza kokularının adeta iskeletini oluşturuyor. ");
        }

        // 5. Sevilmeyen Notalar
        if (disliked.Any())
        {
            var dislikedAccords = disliked
                .SelectMany(p => new[] { p.MainAccord1, p.MainAccord2, p.MainAccord3 })
                .Where(a => !string.IsNullOrEmpty(a))
                .GroupBy(a => a)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Distinct()
                .Where(a => !likedAccords.Contains(a))
                .Take(2)
                .ToList();

            if (dislikedAccords.Any())
            {
                summary.Append($"Tüm bunların aksine; burnunu yoran '{string.Join("' ve '", dislikedAccords)}' gibi notalardan ve bu tarzın getirdiği auralardan kesinlikle uzak duruyorsun. ");
            }
        }

        summary.Append("Mevcut verilerle koku DNA'nı analiz ettik ve zevkine birebir uyacağından emin olduğumuz bu kusursuz seçkiyi senin için hazırladık:");

        return summary.ToString();
    }
}

public class RecommendationDto
{
    public Perfume Perfume { get; set; } = null!;
    public string Explanation { get; set; } = string.Empty;
}
