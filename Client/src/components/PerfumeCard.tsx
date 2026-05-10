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

  const formatString = (str?: string) => {
    if (!str) return '';
    return str
      .replace(/-/g, ' ')
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  };

  const getPriceRange = (price?: number) => {
    if (!price) return null;
    if (price === 350) return "$250 - $500";
    if (price === 135) return "$90 - $180";
    if (price === 30) return "$15 - $45";
    if (price === 100) return "$60 - $140";
    if (price === 85) return "$50 - $120";
    return "$40 - $100";
  };

  return (
    <div className="perfume-card">
      <div className="perfume-image-wrapper">
        <img 
          src={perfume.imageUrl || "/perfume-placeholder.png"} 
          alt={formatString(perfume.name)} 
          onError={(e) => { e.currentTarget.src = "/perfume-placeholder.png" }}
          className="perfume-image" 
        />
      </div>
      <div className="perfume-info">
        <h3>{formatString(perfume.name)}</h3>
        <p className="brand">{formatString(perfume.brand)}</p>
        <div className="details">
          {perfume.price && <span style={{ background: 'rgba(16, 185, 129, 0.1)', color: '#10b981', border: '1px solid rgba(16, 185, 129, 0.2)' }}>{getPriceRange(perfume.price)}</span>}
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
