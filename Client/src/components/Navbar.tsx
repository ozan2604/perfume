import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { useState, useEffect } from 'react';

const Navbar: React.FC = () => {
  const { token, name, logout } = useAuthStore();
  const navigate = useNavigate();
  const location = useLocation();
  const [theme, setTheme] = useState(localStorage.getItem('theme') || 'light');

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  const toggleTheme = () => {
    setTheme((prevTheme) => (prevTheme === 'dark' ? 'light' : 'dark'));
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const isActive = (path: string) => location.pathname === path ? 'active' : '';

  return (
    <nav className="navbar">
      <div className="navbar-container">
        <Link to="/" className="navbar-logo">
          ✨ PerfumeAI
        </Link>
        <div className="navbar-links">
          <button onClick={toggleTheme} className="theme-toggle-btn">
            {theme === 'dark' ? '☀️ Açık Mod' : '🌙 Karanlık Mod'}
          </button>
          {token ? (
            <>
              <Link to="/" className={`navbar-link ${isActive('/')}`}>Ana Sayfa</Link>
              <Link to="/recommendations" className={`navbar-link ${isActive('/recommendations')}`}>Sana Özel</Link>
              <Link to="/interactions" className={`navbar-link ${isActive('/interactions')}`}>Etkileşimlerim</Link>
              <span className="navbar-user">Selam, {name}</span>
              <button onClick={handleLogout} className="navbar-button">Çıkış Yap</button>
            </>
          ) : (
            <>
              <Link to="/login" className={`navbar-link ${isActive('/login')}`}>Giriş Yap</Link>
              <Link to="/register" className={`navbar-link ${isActive('/register')}`}>Kayıt Ol</Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
