import axios from 'axios';
import { useAuthStore } from '../store/authStore';

const BASE_URL = 'http://localhost:5016/api';

const api = axios.create({ baseURL: BASE_URL });

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Auth
export const register = (data: {
  email: string; password: string; name: string;
  surname: string; gender: string; birthDate?: string;
}) => api.post('/auth/register', data);

export const login = (data: { email: string; password: string }) =>
  api.post<{ token: string; userId: string; name: string }>('/auth/login', data);

// Perfumes
export const getPerfumes = (page = 1, pageSize = 20) =>
  api.get<Perfume[]>(`/perfumes?page=${page}&pageSize=${pageSize}`);

export const getRecommendations = (count = 10) =>
  api.get<Perfume[]>(`/perfumes/recommendations?count=${count}`);

export const interact = (perfumeId: string, isLiked: boolean) =>
  api.post('/perfumes/interact', { perfumeId, isLiked });

export interface Perfume {
  id: string;
  name: string;
  brand: string;
  gender?: string;
  rating?: number;
  year?: number;
  country?: string;
}
