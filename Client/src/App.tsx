import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import RecommendationsPage from './pages/RecommendationsPage';
import InteractionsPage from './pages/InteractionsPage';
import ProtectedRoute from './components/ProtectedRoute';
import './index.css';

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <div className="app">
        <Navbar />
        <main className="main-content">
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            
            <Route element={<ProtectedRoute />}>
              <Route path="/" element={<HomePage />} />
              <Route path="/recommendations" element={<RecommendationsPage />} />
              <Route path="/interactions" element={<InteractionsPage />} />
            </Route>
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
};

export default App;
