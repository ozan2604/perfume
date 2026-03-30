import React, { useState } from 'react';
import { interact } from '../api/client';
import type { Perfume } from '../api/client';

interface PerfumeCardProps {
  perfume: Perfume;
  initialLiked?: boolean | null;
}

const PerfumeCard: React.FC<PerfumeCardProps> = ({ perfume, initialLiked = null }) => {
  const [liked, setLiked] = React.useState<boolean | null>(initialLiked ?? null);
  const [loading, setLoading] = React.useState(false);

  React.useEffect(() => {
    if (initialLiked !== undefined) {
      setLiked(initialLiked);
    }
  }, [initialLiked]);

  const handleInteract = async (isLiked: boolean) => {
    setLoading(true);
    try {
      await interact(perfume.id, isLiked);
      setLiked(isLiked);
    } catch (error) {
      console.error('Etkileşim hatası:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="perfume-card">
      <div className="perfume-info">
        <h3>{perfume.name}</h3>
        <p className="brand">{perfume.brand}</p>
        <div className="details">
          {perfume.year && <span>{perfume.year}</span>}
          {perfume.gender && <span>{perfume.gender}</span>}
          {perfume.country && <span className="country">🌍 {perfume.country}</span>}
          {perfume.rating && <span className="rating">⭐ {perfume.rating}</span>}
        </div>
        
        {perfume.explanation && (
          <div className="perfume-explanation">
            <span className="explanation-icon">💡</span>
            <span className="explanation-text">{perfume.explanation}</span>
          </div>
        )}
      </div>
      <div className="perfume-actions-container">
        <div className="perfume-actions">
          <button 
            onClick={() => handleInteract(true)} 
            disabled={loading || liked === true}
            className={`action-btn like ${liked === true ? 'active' : ''}`}
          >
            ❤️ {liked === true ? 'Beğenildi' : 'Beğen'}
          </button>
          <button 
            onClick={() => handleInteract(false)} 
            disabled={loading || liked === false}
            className={`action-btn dislike ${liked === false ? 'active' : ''}`}
          >
            👎 {liked === false ? 'Beğenilmedi' : 'Beğenme'}
          </button>
        </div>
        
        {perfume.url && (
          <a href={perfume.url} target="_blank" rel="noopener noreferrer" className="perfume-link">
            🔗 Ürünü İncele
          </a>
        )}
      </div>
    </div>
  );
};

export default PerfumeCard;
