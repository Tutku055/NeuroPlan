import React, { createContext, useContext, useState, useEffect } from 'react';
import { jwtDecode } from 'jwt-decode';

interface DecodedToken {
  Id: string;
  sub: string;
  role: string;
  Permissions: string;
  exp: number;
}

interface AuthContextType {
  token: string | null;
  userId: string | null;
  role: string | null;
  permissions: string[];
  login: (token: string, role: string, permissions: string[]) => void;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType>({
  token: null,
  userId: null,
  role: null,
  permissions: [],
  login: () => {},
  logout: () => {},
  isAuthenticated: false,
});

export const useAuth = () => useContext(AuthContext);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const [userId, setUserId] = useState<string | null>(localStorage.getItem('userId'));
  const [role, setRole] = useState<string | null>(localStorage.getItem('role'));
  const [permissions, setPermissions] = useState<string[]>(() => {
    const raw = localStorage.getItem('permissions');
    if (!raw) return [];

    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) return parsed;
      if (typeof parsed === 'string') {
        return parsed.split(',').map(p => p.trim()).filter(Boolean);
      }
      return [];
    } catch {
      return [];
    }
  });

  useEffect(() => {
    if (token) {
      try {
        const decoded = jwtDecode<DecodedToken>(token);
        // Sync userId from token
        if (decoded.Id && decoded.Id !== userId) {
            setUserId(decoded.Id);
            localStorage.setItem('userId', decoded.Id);
        }
        // Simple expiry check
        if (decoded.exp * 1000 < Date.now()) {
          logout();
        }
      } catch {
        logout();
      }
    }
  }, [token]);

  const login = (newToken: string, newRole: string, newPermissions: string[]) => {
    localStorage.setItem('token', newToken);
    localStorage.setItem('role', newRole);
    localStorage.setItem('permissions', JSON.stringify(newPermissions));
    setToken(newToken);
    
    try {
        const decoded = jwtDecode<DecodedToken>(newToken);
        if (decoded.Id) {
            localStorage.setItem('userId', decoded.Id);
            setUserId(decoded.Id);
        }
    } catch {}

    setRole(newRole);
    setPermissions(newPermissions);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('role');
    localStorage.removeItem('permissions');
    setToken(null);
    setUserId(null);
    setRole(null);
    setPermissions([]);
  };

  return (
    <AuthContext.Provider value={{ token, userId, role, permissions, login, logout, isAuthenticated: !!token }}>
      {children}
    </AuthContext.Provider>
  );
};
