import React from "react";
import { Outlet, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import {
  LayoutDashboard,
  LogOut,
  Brain,
  User,
  Zap,
  Activity,
  BarChart3,
  Users,
  Shield,
  ClipboardList,
} from "lucide-react";

export const DashboardLayout: React.FC = () => {
  const { role, roleColor, permissions, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const menuItems: { name: string; path: string; icon: React.ReactNode }[] = [];

  if (permissions.includes("ManageProjects")) {
    menuItems.push({
      name: "Projects",
      path: "/projects",
      icon: <LayoutDashboard size={18} />,
    });
  }

  if (permissions.includes("ManageTaskItems")) {
    menuItems.push({
      name: "Tasks",
      path: "/tasks",
      icon: <ClipboardList size={18} />,
    });
  }

  if (permissions.includes("TrackWork")) {
    menuItems.push({
      name: "Worklogs",
      path: "/worklogs",
      icon: <Activity size={18} />,
    });
  }

  if (permissions.includes("ViewStatistics")) {
    menuItems.push({
      name: "Performance",
      path: "/performance",
      icon: <BarChart3 size={18} />,
    });
  }

  if (permissions.includes("ManageUsers")) {
    menuItems.push({
      name: "Users",
      path: "/users",
      icon: <Users size={18} />,
    });
  }

  if (permissions.includes("ManageRoles")) {
    menuItems.push({
      name: "Roles",
      path: "/roles",
      icon: <Shield size={18} />,
    });
  }

  const isActive = (path: string) => location.pathname.startsWith(path);

  const fallbackRoleColor =
    role === "Admin" ? "#a855f7" : role === "Manager" ? "#0ea5e9" : "#10b981";
  const effectiveRoleColor = roleColor || fallbackRoleColor;

  return (
    <div className="app-shell">
      <aside className="sidebar no-print">
        <div
          style={{
            padding: "0.5rem 0.5rem 1.5rem",
            display: "flex",
            flexDirection: "column",
            gap: "0.5rem",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "0.6rem" }}>
            <div
              style={{
                width: 34,
                height: 34,
                borderRadius: 10,
                background: "linear-gradient(135deg, #7c3aed, #0ea5e9)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                boxShadow: "0 4px 12px rgba(124,58,237,0.4)",
                flexShrink: 0,
              }}
            >
              <Brain size={18} color="#fff" />
            </div>
            <span
              className="text-gradient"
              style={{
                fontSize: "1.15rem",
                fontWeight: 700,
                letterSpacing: "-0.03em",
              }}
            >
              NeuroPlan
            </span>
          </div>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.5rem",
              padding: "0.4rem 0.65rem",
              background: "rgba(255,255,255,0.04)",
              border: "1px solid rgba(255,255,255,0.07)",
              borderRadius: 8,
            }}
          >
            <div
              style={{
                width: 22,
                height: 22,
                borderRadius: "50%",
                background: `${effectiveRoleColor}22`,
                border: `1.5px solid ${effectiveRoleColor}55`,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <User size={11} color={effectiveRoleColor} />
            </div>
            <span
              style={{ fontSize: "0.75rem", color: "var(--text-secondary)" }}
            >
              Signed in as
            </span>
            <span
              style={{
                fontSize: "0.75rem",
                fontWeight: 600,
                color: effectiveRoleColor,
                marginLeft: "auto",
              }}
            >
              {role}
            </span>
          </div>
        </div>

        <div className="divider" />
        <p
          style={{
            fontSize: "0.65rem",
            fontWeight: 600,
            color: "var(--text-muted)",
            textTransform: "uppercase",
            letterSpacing: "0.08em",
            padding: "0.75rem 0.5rem 0.25rem",
          }}
        >
          Navigation
        </p>

        <nav
          style={{
            display: "flex",
            flexDirection: "column",
            gap: "0.25rem",
            flex: 1,
          }}
        >
          {menuItems.map((item) => (
            <button
              key={item.path}
              className={`nav-item ${isActive(item.path) ? "active" : ""}`}
              onClick={() => navigate(item.path)}
            >
              {item.icon}
              {item.name}
              {isActive(item.path) && (
                <Zap
                  size={12}
                  style={{
                    marginLeft: "auto",
                    color: "var(--accent-primary)",
                    opacity: 0.8,
                  }}
                />
              )}
            </button>
          ))}
        </nav>

        <div className="divider" />
        <button
          onClick={handleLogout}
          style={{
            display: "flex",
            alignItems: "center",
            gap: "0.85rem",
            padding: "0.7rem 0.9rem",
            background: "rgba(244, 63, 94, 0.07)",
            color: "#fb7185",
            border: "1px solid rgba(244, 63, 94, 0.15)",
            borderRadius: "var(--radius-md)",
            cursor: "pointer",
            fontSize: "0.875rem",
            fontWeight: 500,
            width: "100%",
            transition: "all 0.2s ease",
            fontFamily: "var(--font-family)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background =
              "rgba(244, 63, 94, 0.14)";
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "rgba(244, 63, 94, 0.3)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background =
              "rgba(244, 63, 94, 0.07)";
            (e.currentTarget as HTMLButtonElement).style.borderColor =
              "rgba(244, 63, 94, 0.15)";
          }}
        >
          <LogOut size={16} />
          Sign Out
        </button>

        <p
          style={{
            fontSize: "0.65rem",
            color: "var(--text-muted)",
            textAlign: "center",
            marginTop: "1rem",
            paddingBottom: "0.5rem",
          }}
        >
          NeuroPlan v0.1.0
        </p>
      </aside>

      <main className="main-content">
        <div className="animate-fade-y" style={{ flex: 1 }}>
          <Outlet />
        </div>
      </main>
    </div>
  );
};
