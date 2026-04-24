import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { Lock, User, Target, ArrowRight, Brain } from "lucide-react";
import axios from "axios";

export const Login: React.FC = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");
    try {
      const response = await axios.post(
        "http://127.0.0.1:5000/api/auth/login",
        { username, password },
      );
      const { token, role, permissions } = response.data;

      // Fallback in case the API doesn't camelCase
      const actualToken = token || response.data.Token;
      const actualRole = role || response.data.Role;
      const actualPermissions = permissions || response.data.Permissions;

      login(actualToken, actualRole, actualPermissions);
      navigate("/");
    } catch (err: any) {
      setError(
        err.response?.data?.message ||
          "Authentication failed. Try admin/manager/worker, or use your account email.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        display: "flex",
        minHeight: "100%",
        width: "100%",
        overflow: "auto",
      }}
    >
      {/* ── Left decorative panel ── */}
      <div
        style={{
          flex: 1,
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          alignItems: "center",
          padding: "3rem",
          position: "relative",
          overflow: "hidden",
          minWidth: 0,
        }}
        className="no-print"
      >
        {/* Decorative orbs */}
        <div
          style={{
            position: "absolute",
            top: "15%",
            left: "20%",
            width: 320,
            height: 320,
            borderRadius: "50%",
            background:
              "radial-gradient(circle, rgba(124,58,237,0.18) 0%, transparent 70%)",
            filter: "blur(40px)",
            pointerEvents: "none",
          }}
        />
        <div
          style={{
            position: "absolute",
            bottom: "20%",
            right: "15%",
            width: 250,
            height: 250,
            borderRadius: "50%",
            background:
              "radial-gradient(circle, rgba(14,165,233,0.14) 0%, transparent 70%)",
            filter: "blur(36px)",
            pointerEvents: "none",
          }}
        />
        {/* Grid overlay */}
        <div
          style={{
            position: "absolute",
            inset: 0,
            backgroundImage: `linear-gradient(rgba(255,255,255,0.025) 1px, transparent 1px),
                            linear-gradient(90deg, rgba(255,255,255,0.025) 1px, transparent 1px)`,
            backgroundSize: "40px 40px",
            pointerEvents: "none",
          }}
        />

        <div
          className="animate-fade-y"
          style={{
            position: "relative",
            zIndex: 1,
            textAlign: "center",
            maxWidth: 460,
          }}
        >
          <div
            style={{
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              width: 72,
              height: 72,
              borderRadius: 22,
              background: "linear-gradient(135deg, #7c3aed 0%, #0ea5e9 100%)",
              boxShadow: "0 8px 32px rgba(124,58,237,0.45)",
              marginBottom: "1.5rem",
            }}
          >
            <Brain size={36} color="#fff" />
          </div>
          <h1
            className="text-gradient"
            style={{
              marginBottom: "0.75rem",
              fontSize: "clamp(2rem, 4vw, 3rem)",
            }}
          >
            NeuroPlan
          </h1>
          <p
            style={{
              color: "var(--text-secondary)",
              fontSize: "1.05rem",
              lineHeight: 1.7,
              maxWidth: 380,
              margin: "0 auto",
            }}
          >
            AI-powered project intelligence for modern engineering teams.
          </p>

          <div
            style={{
              display: "flex",
              gap: "1.5rem",
              justifyContent: "center",
              marginTop: "2.5rem",
              flexWrap: "wrap",
            }}
          >
            {[
              { label: "AI Forecasting", color: "#7c3aed" },
              { label: "QR Tracking", color: "#0ea5e9" },
              { label: "Role-Based", color: "#10b981" },
            ].map((f) => (
              <div
                key={f.label}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "0.5rem",
                  fontSize: "0.82rem",
                  color: "var(--text-secondary)",
                }}
              >
                <span
                  style={{
                    width: 6,
                    height: 6,
                    borderRadius: "50%",
                    background: f.color,
                    flexShrink: 0,
                  }}
                />
                {f.label}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* ── Right: login card ── */}
      <div
        style={{
          width: "100%",
          maxWidth: 440,
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          padding: "2rem 2.5rem",
          borderLeft: "1px solid var(--glass-border)",
          background: "rgba(10, 10, 20, 0.5)",
          backdropFilter: "blur(20px)",
          WebkitBackdropFilter: "blur(20px)",
          minHeight: "100%",
        }}
      >
        <div
          className="animate-fade-y"
          style={{ width: "100%", maxWidth: 360, margin: "0 auto" }}
        >
          <div style={{ marginBottom: "2rem" }}>
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: "0.6rem",
                marginBottom: "0.5rem",
              }}
            >
              <Target size={18} color="var(--accent-primary)" />
              <span
                style={{
                  fontSize: "0.8rem",
                  color: "var(--text-muted)",
                  fontWeight: 500,
                }}
              >
                NEUROPLAN
              </span>
            </div>
            <h2
              style={{
                fontSize: "1.6rem",
                fontWeight: 700,
                marginBottom: "0.4rem",
              }}
            >
              Welcome back
            </h2>
            <p style={{ color: "var(--text-muted)", fontSize: "0.9rem" }}>
              Sign in to access your workspace
            </p>
          </div>

          {error && (
            <div
              style={{
                padding: "0.75rem 1rem",
                background: "rgba(244, 63, 94, 0.1)",
                border: "1px solid rgba(244, 63, 94, 0.25)",
                color: "#fb7185",
                borderRadius: "var(--radius-md)",
                marginBottom: "1.5rem",
                fontSize: "0.85rem",
                display: "flex",
                alignItems: "flex-start",
                gap: "0.5rem",
              }}
            >
              <span style={{ flexShrink: 0, marginTop: 1 }}>⚠</span>
              {error}
            </div>
          )}

          <form
            onSubmit={handleLogin}
            style={{ display: "flex", flexDirection: "column", gap: "1rem" }}
          >
            <div>
              <label
                style={{
                  display: "block",
                  fontSize: "0.8rem",
                  fontWeight: 500,
                  color: "var(--text-secondary)",
                  marginBottom: "0.4rem",
                }}
              >
                Username or Email
              </label>
              <div style={{ position: "relative" }}>
                <div
                  style={{
                    position: "absolute",
                    left: "0.85rem",
                    top: "50%",
                    transform: "translateY(-50%)",
                    color: "var(--text-muted)",
                    display: "flex",
                    alignItems: "center",
                    pointerEvents: "none",
                  }}
                >
                  <User size={16} />
                </div>
                <input
                  className="input-field"
                  type="text"
                  placeholder="e.g. admin or user@example.com"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  autoComplete="username"
                  style={{ paddingLeft: "2.6rem" }}
                />
              </div>
            </div>

            <div>
              <label
                style={{
                  display: "block",
                  fontSize: "0.8rem",
                  fontWeight: 500,
                  color: "var(--text-secondary)",
                  marginBottom: "0.4rem",
                }}
              >
                Password
              </label>
              <div style={{ position: "relative" }}>
                <div
                  style={{
                    position: "absolute",
                    left: "0.85rem",
                    top: "50%",
                    transform: "translateY(-50%)",
                    color: "var(--text-muted)",
                    display: "flex",
                    alignItems: "center",
                    pointerEvents: "none",
                  }}
                >
                  <Lock size={16} />
                </div>
                <input
                  className="input-field"
                  type="password"
                  placeholder="Enter password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                  style={{ paddingLeft: "2.6rem" }}
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                gap: "0.5rem",
                width: "100%",
                padding: "0.8rem",
                marginTop: "0.5rem",
                background: loading
                  ? "rgba(124,58,237,0.5)"
                  : "linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%)",
                color: "#fff",
                border: "none",
                borderRadius: "var(--radius-md)",
                fontSize: "0.95rem",
                fontWeight: 600,
                cursor: loading ? "not-allowed" : "pointer",
                boxShadow: loading ? "none" : "0 4px 20px rgba(124,58,237,0.4)",
                transition: "all 0.2s ease",
                fontFamily: "var(--font-family)",
              }}
            >
              {loading ? (
                <>
                  <span
                    className="animate-spin"
                    style={{
                      width: 16,
                      height: 16,
                      border: "2px solid rgba(255,255,255,0.3)",
                      borderTopColor: "#fff",
                      borderRadius: "50%",
                      display: "inline-block",
                    }}
                  />
                  Authenticating…
                </>
              ) : (
                <>
                  Access Workspace
                  <ArrowRight size={16} />
                </>
              )}
            </button>
          </form>

          <p
            style={{
              marginTop: "1.5rem",
              fontSize: "0.75rem",
              color: "var(--text-muted)",
              textAlign: "center",
              lineHeight: 1.7,
            }}
          >
            Demo aliases:{" "}
            <span style={{ color: "var(--text-secondary)" }}>admin</span>,{" "}
            <span style={{ color: "var(--text-secondary)" }}>manager</span>,{" "}
            <span style={{ color: "var(--text-secondary)" }}>worker</span>.
            Created users can sign in with email + password.
          </p>
        </div>
      </div>
    </div>
  );
};
