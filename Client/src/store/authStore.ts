import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  token: string | null;
  userId: string | null;
  name: string | null;
  setAuth: (token: string, userId: string, name: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      userId: null,
      name: null,
      setAuth: (token, userId, name) => set({ token, userId, name }),
      logout: () => set({ token: null, userId: null, name: null }),
    }),
    { name: 'perfume-auth' }
  )
);
