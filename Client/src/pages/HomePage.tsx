import React, { useEffect, useState } from 'react';
import { getPerfumes, getInteractions, getCountries } from '../api/client';
import type { Perfume } from '../api/client';
import PerfumeCard from '../components/PerfumeCard';

const HomePage: React.FC = () => {
  const [perfumes, setPerfumes] = useState<Perfume[]>([]);
  const [interactions, setInteractions] = useState<Record<string, boolean>>({});
  const [countries, setCountries] = useState<string[]>([]);
  
  // Filter States
  const [selectedGender, setSelectedGender] = useState<string>('');
  const [selectedCountry, setSelectedCountry] = useState<string>('');
  const [sortBy, setSortBy] = useState<string>('');

  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchCountries();
  }, []);

  useEffect(() => {
    fetchPerfumes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, selectedGender, selectedCountry, sortBy]);

  const fetchCountries = async () => {
    try {
      const res = await getCountries();
      setCountries(res.data);
    } catch (err) {
      console.error('Ülkeler listesi alınamadı:', err);
    }
  };

  const fetchPerfumes = async () => {
    setLoading(true);
    try {
      const perfumesRes = await getPerfumes(page, 20, selectedGender, selectedCountry, sortBy);
      setPerfumes(perfumesRes.data);

      try {
        const interactionsRes = await getInteractions();
        const normalized: Record<string, boolean> = {};
        if (interactionsRes.data) {
          Object.keys(interactionsRes.data).forEach(k => {
            normalized[k.toLowerCase()] = interactionsRes.data[k];
          });
        }
        setInteractions(normalized);
      } catch (intErr) {
        console.warn('Etkileşimler çekilemedi (Belki de misafir kullanıcı)');
      }
    } catch (error) {
      console.error('Parfüm yükleme hatası:', error);
    } finally {
      setLoading(false);
    }
  };

  const resetFilters = () => {
    setSelectedGender('');
    setSelectedCountry('');
    setSortBy('');
    setPage(1);
  };

  return (
    <div className="container">
      <div className="modern-filter-container">
        <div className="filter-section pills-section">
          <div className="pills-group">
            <button className={`pill-btn ${selectedGender === '' ? 'active' : ''}`} onClick={() => { setSelectedGender(''); setPage(1); }}>Tümü</button>
            <button className={`pill-btn ${selectedGender === 'men' ? 'active' : ''}`} onClick={() => { setSelectedGender('men'); setPage(1); }}>Erkek</button>
            <button className={`pill-btn ${selectedGender === 'women' ? 'active' : ''}`} onClick={() => { setSelectedGender('women'); setPage(1); }}>Kadın</button>
            <button className={`pill-btn ${selectedGender === 'unisex' ? 'active' : ''}`} onClick={() => { setSelectedGender('unisex'); setPage(1); }}>Unisex</button>
          </div>
        </div>

        <div className="filter-section select-section">
          <div className="styled-select-container">
            <span className="select-icon">🌍</span>
            <select 
              className="styled-select"
              value={selectedCountry} 
              onChange={(e) => { setSelectedCountry(e.target.value); setPage(1); }}
            >
              <option value="">Tüm Ülkeler</option>
              {countries.map(c => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </div>

          <div className="styled-select-container">
            <span className="select-icon">⚡</span>
            <select 
              className="styled-select"
              value={sortBy} 
              onChange={(e) => { setSortBy(e.target.value); setPage(1); }}
            >
              <option value="">A-Z Sıralama</option>
              <option value="rating_desc">En Popüler</option>
            </select>
          </div>
          
          {(selectedGender || selectedCountry || sortBy) && (
            <button className="modern-reset-btn" onClick={resetFilters}>
              Temizle
            </button>
          )}
        </div>
      </div>

      {loading ? (
        <div className="loader">Yükleniyor...</div>
      ) : (
        <>
          {perfumes.length === 0 ? (
            <div className="empty-state">Bu filtrelere uygun parfüm bulunamadı.</div>
          ) : (
            <div className="perfume-grid">
              {perfumes.map((perfume) => (
                <PerfumeCard 
                  key={perfume.id} 
                  perfume={perfume} 
                  initialLiked={interactions[perfume.id?.toLowerCase()]} 
                />
              ))}
            </div>
          )}
          
          <div className="pagination">
            <button 
              onClick={() => setPage(page - 1)} 
              disabled={page === 1}
              className="page-btn"
            >
              Geri
            </button>
            <span className="page-info">Sayfa {page}</span>
            <button 
              onClick={() => setPage(page + 1)} 
              disabled={perfumes.length < 20}
              className="page-btn"
            >
              İleri
            </button>
          </div>
        </>
      )}
    </div>
  );
};

export default HomePage;
