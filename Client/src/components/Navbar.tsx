import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const Navbar: React.FC = () => {
  const { token, name, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="navbar">
      <div className="navbar-container">
        <Link to="/" className="navbar-logo">
          ✨ PerfumeAI
        </Link>
        <div className="navbar-links">
          {token ? (
            <>
              <Link to="/" className="navbar-link">Ana Sayfa</Link>
              <Link to="/recommendations" className="navbar-link">Sana Özel</Link>
              <span className="navbar-user">Selam, {name}</span>
              <button onClick={handleLogout} className="navbar-button">Çıkış Yap</button>
            </>
          ) : (
            <>
              <Link to="/login" className="navbar-link">Giriş Yap</Link>
              <Link to="/register" className="navbar-link">Kayıt Ol</Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
