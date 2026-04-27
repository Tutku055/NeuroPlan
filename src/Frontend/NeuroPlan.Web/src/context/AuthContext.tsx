import React, { createContext, useContext, useState, useEffect } from "react";
import { jwtDecode } from "jwt-decode";

interface DecodedToken {
  Id: string;
  sub: string;
  role: string;
  Permissions: string;
  RoleColor?: string;
  exp: number;
}

const normalizeHexColor = (value?: string | null) => {
  if (!value) return null;
  const trimmed = value.trim();
  if (!trimmed) return null;
  return trimmed.startsWith("#") ? trimmed : `#${trimmed}`;
};

interface AuthContextType {
  token: string | null;
  userId: string | null;
  role: string | null;
  roleColor: string | null;
  permissions: string[];
  login: (
    token: string,
    role: string,
    permissions: string[],
    roleColor?: string | null,
  ) => void;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType>({
  token: null,
  userId: null,
  role: null,
  roleColor: null,
  permissions: [],
  login: () => {},
  logout: () => {},
  isAuthenticated: false,
});

export const useAuth = () => useContext(AuthContext);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [token, setToken] = useState<string | null>(
    localStorage.getItem("token"),
  );
  const [userId, setUserId] = useState<string | null>(
    localStorage.getItem("userId"),
  );
  const [role, setRole] = useState<string | null>(localStorage.getItem("role"));
  const [roleColor, setRoleColor] = useState<string | null>(
    localStorage.getItem("roleColor"),
  );
  const [permissions, setPermissions] = useState<string[]>(() => {
    const raw = localStorage.getItem("permissions");
    if (!raw) return [];

    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) return parsed;
      if (typeof parsed === "string") {
        return parsed
          .split(",")
          .map((p) => p.trim())
          .filter(Boolean);
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
          localStorage.setItem("userId", decoded.Id);
        }

        const decodedRoleColor = normalizeHexColor(decoded.RoleColor);
        if (decodedRoleColor && decodedRoleColor !== roleColor) {
          setRoleColor(decodedRoleColor);
          localStorage.setItem("roleColor", decodedRoleColor);
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

  const login = (
    newToken: string,
    newRole: string,
    newPermissions: string[],
    newRoleColor?: string | null,
  ) => {
    localStorage.setItem("token", newToken);
    localStorage.setItem("role", newRole);

    const normalizedRoleColor = normalizeHexColor(newRoleColor);
    if (normalizedRoleColor) {
      localStorage.setItem("roleColor", normalizedRoleColor);
    } else {
      localStorage.removeItem("roleColor");
    }
    localStorage.setItem("permissions", JSON.stringify(newPermissions));
    setToken(newToken);

    try {
      const decoded = jwtDecode<DecodedToken>(newToken);
      if (decoded.Id) {
        localStorage.setItem("userId", decoded.Id);
        setUserId(decoded.Id);
      }
    } catch {}

    setRole(newRole);
    setRoleColor(normalizedRoleColor);
    setPermissions(newPermissions);
  };

  const logout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("userId");
    localStorage.removeItem("role");
    localStorage.removeItem("roleColor");
    localStorage.removeItem("permissions");
    setToken(null);
    setUserId(null);
    setRole(null);
    setRoleColor(null);
    setPermissions([]);
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        userId,
        role,
        roleColor,
        permissions,
        login,
        logout,
        isAuthenticated: !!token,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
