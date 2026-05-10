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
  const [sortBy, setSortBy] = useState<string>('rating_desc');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [searchInput, setSearchInput] = useState<string>(''); // For the input field

  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchCountries();
  }, []);

  useEffect(() => {
    fetchPerfumes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, selectedGender, selectedCountry, sortBy, searchQuery]);

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
      const perfumesRes = await getPerfumes(page, 20, selectedGender, selectedCountry, sortBy, searchQuery);
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

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setSearchQuery(searchInput);
    setPage(1);
  };

  const resetFilters = () => {
    setSelectedGender('');
    setSelectedCountry('');
    setSortBy('rating_desc');
    setSearchQuery('');
    setSearchInput('');
    setPage(1);
  };

  return (
    <div className="container">
      <div className="modern-filter-container" style={{ flexDirection: 'column', gap: '1rem' }}>
        
        <form className="search-section" onSubmit={handleSearch} style={{ display: 'flex', gap: '0.5rem', width: '100%' }}>
          <div className="styled-input-container" style={{ flex: 1, position: 'relative' }}>
            <span className="search-icon" style={{ position: 'absolute', left: '1rem', top: '50%', transform: 'translateY(-50%)', opacity: 0.5 }}>🔍</span>
            <input 
              type="text" 
              placeholder="Parfüm, marka veya ülke ara..." 
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              style={{ width: '100%', padding: '0.75rem 1rem 0.75rem 2.5rem', borderRadius: '12px', border: '1px solid var(--border)', background: 'var(--card-bg)', color: 'var(--text)', outline: 'none' }}
            />
          </div>
          <button type="submit" className="action-btn" style={{ background: 'var(--primary)', color: 'white', padding: '0 1.5rem', borderRadius: '12px' }}>
            Ara
          </button>
        </form>

        <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', gap: '1rem', width: '100%', alignItems: 'center' }}>
          <div className="filter-section pills-section" style={{ margin: 0 }}>
            <div className="pills-group">
              <button className={`pill-btn ${selectedGender === '' ? 'active' : ''}`} onClick={() => { setSelectedGender(''); setPage(1); }}>Tümü</button>
              <button className={`pill-btn ${selectedGender === 'men' ? 'active' : ''}`} onClick={() => { setSelectedGender('men'); setPage(1); }}>Erkek</button>
              <button className={`pill-btn ${selectedGender === 'women' ? 'active' : ''}`} onClick={() => { setSelectedGender('women'); setPage(1); }}>Kadın</button>
              <button className={`pill-btn ${selectedGender === 'unisex' ? 'active' : ''}`} onClick={() => { setSelectedGender('unisex'); setPage(1); }}>Unisex</button>
            </div>
          </div>

          <div className="filter-section select-section" style={{ flexDirection: 'row', flexWrap: 'wrap', gap: '1rem', margin: 0 }}>
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
                <option value="rating_desc">En Popüler</option>
                <option value="">A-Z Sıralama</option>
                <option value="price_asc">Ucuzdan Pahalıya</option>
                <option value="price_desc">Pahalıdan Ucuza</option>
              </select>
            </div>
            
            {(selectedGender || selectedCountry || sortBy !== 'rating_desc' || searchQuery) && (
              <button className="modern-reset-btn" onClick={resetFilters}>
                Temizle
              </button>
            )}
          </div>
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
