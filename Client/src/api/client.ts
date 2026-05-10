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
export const getPerfumes = (page = 1, pageSize = 20, gender?: string, country?: string, sortBy?: string, search?: string) => {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });
  if (gender) params.append('gender', gender);
  if (country) params.append('country', country);
  if (sortBy) params.append('sortBy', sortBy);
  if (search) params.append('search', search);

  return api.get<Perfume[]>(`/perfumes?${params.toString()}`);
};

export const getCountries = () => api.get<string[]>('/perfumes/countries');

export const getRecommendations = (count = 10) =>
  api.get<Perfume[]>(`/perfumes/recommendations?count=${count}`);

export const getProfileSummary = () =>
  api.get<{ summary: string }>('/perfumes/profile-summary');

export const getInteractions = () =>
  api.get<Record<string, boolean>>('/perfumes/interactions');

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
  url?: string;
  imageUrl?: string;
  explanation?: string;
  price?: number;
}

export interface MyInteraction {
  perfume: Perfume;
  isLiked: boolean;
  interactionDate: string;
}

export const getMyInteractions = () =>
  api.get<MyInteraction[]>('/perfumes/my-interactions');
