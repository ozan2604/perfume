import React, { useEffect, useState } from 'react';
import { getRecommendations } from '../api/client';
import type { Perfume } from '../api/client';
import PerfumeCard from '../components/PerfumeCard';

const RecommendationsPage: React.FC = () => {
  const [recommendations, setRecommendations] = useState<Perfume[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchRecommendations();
  }, []);

  const fetchRecommendations = async () => {
    setLoading(true);
    try {
      const response = await getRecommendations(12);
      setRecommendations(response.data);
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
            <PerfumeCard key={perfume.id} perfume={perfume} />
          ))}
        </div>
      )}
    </div>
  );
};

export default RecommendationsPage;
