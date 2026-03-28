import React, { useEffect, useState } from 'react';
import { getMyInteractions } from '../api/client';
import type { MyInteraction } from '../api/client';
import PerfumeCard from '../components/PerfumeCard';

const InteractionsPage: React.FC = () => {
  const [interactions, setInteractions] = useState<MyInteraction[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'liked' | 'disliked'>('liked');

  useEffect(() => {
    fetchMyInteractions();
  }, []);

  const fetchMyInteractions = async () => {
    setLoading(true);
    try {
      const response = await getMyInteractions();
      setInteractions(response.data);
    } catch (error) {
      console.error('Etkileşim geçmişi yükleme hatası:', error);
    } finally {
      setLoading(false);
    }
  };

  const liked = interactions.filter(i => i.isLiked);
  const disliked = interactions.filter(i => !i.isLiked);
  const visibleInteractions = activeTab === 'liked' ? liked : disliked;

  return (
    <div className="container">
      <header className="page-header" style={{ marginBottom: '1rem' }}>
        <h1>Etkileşimlerim</h1>
      </header>

      <div className="tabs-container">
        <button 
          className={`tab-button ${activeTab === 'liked' ? 'active' : ''}`}
          onClick={() => setActiveTab('liked')}
        >
          Beğendiklerim ❤️ ({liked.length})
        </button>
        <button 
          className={`tab-button ${activeTab === 'disliked' ? 'active' : ''}`}
          onClick={() => setActiveTab('disliked')}
        >
          Beğenmediklerim 👎 ({disliked.length})
        </button>
      </div>

      {loading ? (
        <div className="loader">Etkileşimler yükleniyor...</div>
      ) : interactions.length === 0 ? (
        <div className="empty-state">
          <h3>Henüz bir etkileşimin yok.</h3>
          <p>Parfümleri keşfetmeye başla ve favorilerini belirle!</p>
        </div>
      ) : visibleInteractions.length === 0 ? (
        <div className="empty-state">
          <p>Bu sekmede henüz bir etkileşimin bulunmuyor.</p>
        </div>
      ) : (
        <div className="perfume-grid">
          {visibleInteractions.map(({ perfume, isLiked }) => (
            <PerfumeCard key={perfume.id} perfume={perfume} initialLiked={isLiked} />
          ))}
        </div>
      )}
    </div>
  );
};

export default InteractionsPage;
