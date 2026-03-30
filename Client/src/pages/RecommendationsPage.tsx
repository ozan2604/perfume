import React, { useEffect, useState } from 'react';
import { getRecommendations, getInteractions, getProfileSummary } from '../api/client';
import type { Perfume } from '../api/client';
import PerfumeCard from '../components/PerfumeCard';

const RecommendationsPage: React.FC = () => {
  const [recommendations, setRecommendations] = useState<Perfume[]>([]);
  const [interactions, setInteractions] = useState<Record<string, boolean>>({});
  const [profileSummary, setProfileSummary] = useState<string>('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchRecommendations();
  }, []);

  const fetchRecommendations = async () => {
    setLoading(true);
    try {
      const recRes = await getRecommendations(12);
      setRecommendations(recRes.data);

      try {
        const summaryRes = await getProfileSummary();
        if (summaryRes.data?.summary) {
          setProfileSummary(summaryRes.data.summary);
        }
      } catch (sumErr) {
        console.warn('Profil özeti alınamadı.', sumErr);
      }

      try {
        const intRes = await getInteractions();
        const normalized: Record<string, boolean> = {};
        if (intRes.data) {
          Object.keys(intRes.data).forEach(k => {
            normalized[k.toLowerCase()] = intRes.data[k];
          });
        }
        setInteractions(normalized);
      } catch (err) {
        console.warn('Etkileşimler hata aldı.');
      }
    } catch (error) {
      console.error('Öneriler yükleme hatası:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      {profileSummary && !loading && (
        <div className="profile-summary-banner">
          <div className="summary-icon">✨</div>
          <div className="summary-content">
            <h3>Senin Koku Profilin</h3>
            <p>{profileSummary}</p>
          </div>
        </div>
      )}

      {loading ? (
        <div className="loader">Öneriler hazırlanıyor...</div>
      ) : recommendations.length === 0 ? (
        <div className="empty-state">
          <h3>Henüz öneri yok.</h3>
          <p>Sana özel öneriler oluşturabilmemiz için birkaç parfümü beğenmelisin.</p>
        </div>
      ) : (
        <div className="perfume-grid">
          {recommendations.map((perfume) => (
            <PerfumeCard
              key={perfume.id}
              perfume={perfume}
              initialLiked={interactions[perfume.id?.toLowerCase()]}
            />
          ))}
        </div>
      )}
    </div>
  );
};

export default RecommendationsPage;
