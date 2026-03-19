import React, { useEffect, useState } from 'react';
import { getPerfumes } from '../api/client';
import type { Perfume } from '../api/client';
import PerfumeCard from '../components/PerfumeCard';

const HomePage: React.FC = () => {
  const [perfumes, setPerfumes] = useState<Perfume[]>([]);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchPerfumes();
  }, [page]);

  const fetchPerfumes = async () => {
    setLoading(true);
    try {
      const response = await getPerfumes(page);
      setPerfumes(response.data);
    } catch (error) {
      console.error('Parfüm yükleme hatası:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <header className="page-header">
        <h1>Parfümleri Keşfet</h1>
        <p>Sevdiğin kokuları bulmak için parfümleri değerlendir.</p>
      </header>

      {loading ? (
        <div className="loader">Yükleniyor...</div>
      ) : (
        <>
          <div className="perfume-grid">
            {perfumes.map((perfume) => (
              <PerfumeCard key={perfume.id} perfume={perfume} />
            ))}
          </div>
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
