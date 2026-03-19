import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { register } from '../api/client';

const RegisterPage: React.FC = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    name: '',
    surname: '',
    gender: 'Male',
    birthDate: ''
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    
    try {
      await register(formData);
      navigate('/login');
    } catch (err: any) {
      setError('Kayıt başarısız. Lütfen bilgilerinizi kontrol edin.');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  return (
    <div className="auth-page">
      <div className="auth-card shadow-lg">
        <h2>Kayıt Ol</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-row">
            <div className="form-group">
              <label>Ad</label>
              <input type="text" name="name" onChange={handleChange} required />
            </div>
            <div className="form-group">
              <label>Soyad</label>
              <input type="text" name="surname" onChange={handleChange} required />
            </div>
          </div>
          <div className="form-group">
            <label>E-posta</label>
            <input type="email" name="email" onChange={handleChange} required />
          </div>
          <div className="form-group">
            <label>Şifre</label>
            <input type="password" name="password" onChange={handleChange} required />
          </div>
          <div className="form-row">
            <div className="form-group">
              <label>Cinsiyet</label>
              <select name="gender" onChange={handleChange}>
                <option value="Male">Erkek</option>
                <option value="Female">Kadın</option>
                <option value="Unisex">Unisex</option>
              </select>
            </div>
            <div className="form-group">
              <label>Doğum Tarihi</label>
              <input type="date" name="birthDate" onChange={handleChange} />
            </div>
          </div>
          {error && <p className="error-message">{error}</p>}
          <button type="submit" disabled={loading} className="auth-btn">
            {loading ? 'Kayıt yapılıyor...' : 'Kayıt Ol'}
          </button>
        </form>
        <p className="auth-footer">
          Zaten hesabın var mı? <Link to="/login">Giriş Yap</Link>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
