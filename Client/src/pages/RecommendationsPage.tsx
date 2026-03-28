import React, { useEffect, useState } from 'react';
import { getRecommendations, getInteractions } from '../api/client';
import type { Perfume } from '../api/client';
import PerfumeCard from '../components/PerfumeCard';

const RecommendationsPage: React.FC = () => {
  const [recommendations, setRecommendations] = useState<Perfume[]>([]);
  const [interactions, setInteractions] = useState<Record<string, boolean>>({});
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
      <header className="page-header">
        <h1>Sana Özel Öneriler</h1>
        <p>Yapay zeka modelimiz senin için bu parfümleri seçti.</p>
      </header>

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
