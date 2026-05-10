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
      const recRes = await getRecommendations(25);
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
      {profileSummary && !loading && Object.keys(interactions).length >= 10 && (
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
      ) : Object.keys(interactions).length < 10 ? (
        <div className="empty-state" style={{ padding: '4rem 2rem', textAlign: 'center', background: 'var(--card-bg)', borderRadius: '16px', border: '1px solid var(--border)', marginTop: '2rem' }}>
          <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>🔒</div>
          <h2>Sana Özel Sayfası Kilitli</h2>
          <p style={{ color: 'var(--text-muted)', marginTop: '1rem', fontSize: '1.1rem', maxWidth: '500px', margin: '1rem auto' }}>
            Koku profilinin haritasını tam olarak çıkarabilmemiz ve sana en uygun eşleşmeleri sunabilmemiz için en az 10 parfüm değerlendirmelisin.
          </p>
          <div style={{ background: 'rgba(124, 58, 237, 0.1)', padding: '1rem', borderRadius: '12px', display: 'inline-block', color: 'var(--primary)', fontWeight: '600' }}>
            Kalan Parfüm: {10 - Object.keys(interactions).length}
          </div>
          <br/>
          <a href="/" className="action-btn" style={{ display: 'inline-block', marginTop: '2rem', padding: '0.75rem 2rem', background: 'var(--primary)', color: 'white', textDecoration: 'none', borderRadius: '12px' }}>
            Ana Sayfaya Dön ve Keşfet
          </a>
        </div>
      ) : recommendations.length === 0 ? (
        <div className="empty-state">
          <h3>Henüz öneri yok.</h3>
          <p>Sistemde sana uygun bir eşleşme bulunamadı, farklı parfümler oylamayı dene.</p>
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
