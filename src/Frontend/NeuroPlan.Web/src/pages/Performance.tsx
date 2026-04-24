import React, { useEffect, useState } from "react";
import { RefreshCw, BarChart3, Clock, Users, FolderOpen } from "lucide-react";
import { GlassPanel } from "../components/UI/GlassPanel";
import { Button } from "../components/UI/Button";
import { api } from "../api/client";

interface PerformanceEntry {
  projectId: string;
  projectName: string;
  userFullName: string;
  totalMinutes: number;
}

const formatHours = (mins: number) => {
  const h = Math.floor(mins / 60);
  const m = Math.round(mins % 60);
  if (h === 0) return `${m}m`;
  return m > 0 ? `${h}h ${m}m` : `${h}h`;
};

export const Performance: React.FC = () => {
  const [data, setData] = useState<PerformanceEntry[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.get("/performance");
      setData(res.data);
    } catch {}
    setLoading(false);
  };

  useEffect(() => {
    fetchData();
  }, []);

  const grouped = data.reduce(
    (acc, e) => {
      if (!acc[e.projectName]) acc[e.projectName] = [];
      acc[e.projectName].push(e);
      return acc;
    },
    {} as Record<string, PerformanceEntry[]>,
  );

  const totalMinutes = data.reduce((s, e) => s + e.totalMinutes, 0);
  const uniqueProjects = new Set(data.map((e) => e.projectId)).size;
  const uniqueUsers = new Set(data.map((e) => e.userFullName)).size;
  const maxMinutes = data.length > 0 ? Math.max(...data.map((e) => e.totalMinutes)) : 1;

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        overflow: "hidden",
      }}
    >
      <div className="page-header">
        <div>
          <h1 style={{ marginBottom: "0.15rem" }}>Performance</h1>
          <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
            Time effort breakdown by project and team member.
          </p>
        </div>
        <div className="page-header-actions">
          <Button
            variant="secondary"
            onClick={fetchData}
            icon={
              <RefreshCw
                size={15}
                className={loading ? "animate-spin" : ""}
              />
            }
          >
            Refresh
          </Button>
        </div>
      </div>

      {!loading && data.length > 0 && (
        <div
          style={{
            display: "flex",
            gap: "0.75rem",
            marginBottom: "1.25rem",
            flexWrap: "wrap",
          }}
        >
          {[
            {
              label: "Projects",
              value: uniqueProjects,
              color: "var(--accent-primary)",
              icon: <FolderOpen size={14} />,
            },
            {
              label: "Contributors",
              value: uniqueUsers,
              color: "var(--accent-secondary)",
              icon: <Users size={14} />,
            },
            {
              label: "Total Effort",
              value: formatHours(totalMinutes),
              color: "var(--accent-emerald)",
              icon: <Clock size={14} />,
            },
          ].map((s) => (
            <div
              key={s.label}
              style={{
                padding: "0.6rem 1rem",
                background: "var(--glass-bg)",
                border: "1px solid var(--glass-border)",
                borderRadius: "var(--radius-md)",
                display: "flex",
                gap: "0.6rem",
                alignItems: "center",
                fontSize: "0.82rem",
              }}
            >
              <span style={{ color: s.color, display: "flex" }}>{s.icon}</span>
              <span
                style={{ color: s.color, fontWeight: 700, fontSize: "1rem" }}
              >
                {s.value}
              </span>
              <span style={{ color: "var(--text-muted)" }}>{s.label}</span>
            </div>
          ))}
        </div>
      )}

      <div style={{ flex: 1, overflowY: "auto" }}>
        {loading && (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              gap: "1rem",
            }}
          >
            {[1, 2, 3].map((i) => (
              <div
                key={i}
                className="skeleton"
                style={{ height: 120, borderRadius: 16 }}
              />
            ))}
          </div>
        )}

        {!loading && data.length === 0 && (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              justifyContent: "center",
              height: 280,
              color: "var(--text-muted)",
              gap: "0.75rem",
              background: "rgba(255,255,255,0.015)",
              border: "1px dashed var(--glass-border)",
              borderRadius: "var(--radius-xl)",
            }}
          >
            <BarChart3 size={36} style={{ opacity: 0.25 }} />
            <p style={{ fontSize: "0.9rem" }}>
              No performance data yet. Start logging work to see stats.
            </p>
          </div>
        )}

        {!loading && data.length > 0 && (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              gap: "1.5rem",
              paddingBottom: "1.5rem",
            }}
          >
            {Object.entries(grouped).map(([projectName, entries]) => (
              <GlassPanel key={projectName} style={{ padding: "1.25rem" }}>
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "0.5rem",
                    marginBottom: "1rem",
                  }}
                >
                  <BarChart3 size={16} color="var(--accent-primary)" />
                  <h3
                    style={{
                      fontSize: "1rem",
                      margin: 0,
                      fontWeight: 600,
                    }}
                  >
                    {projectName}
                  </h3>
                  <span
                    className="tag"
                    style={{ marginLeft: "auto" }}
                  >
                    {formatHours(
                      entries.reduce((s, e) => s + e.totalMinutes, 0),
                    )}{" "}
                    total
                  </span>
                </div>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Team Member</th>
                      <th>Time Spent</th>
                      <th style={{ width: "40%" }}>Effort</th>
                    </tr>
                  </thead>
                  <tbody>
                    {entries.map((e) => (
                      <tr key={e.userFullName}>
                        <td style={{ fontWeight: 500 }}>{e.userFullName}</td>
                        <td>
                          <span
                            style={{
                              color: "var(--accent-secondary)",
                              fontWeight: 600,
                            }}
                          >
                            {formatHours(e.totalMinutes)}
                          </span>
                        </td>
                        <td>
                          <div className="progress-bar-track">
                            <div
                              className="progress-bar-fill"
                              style={{
                                width: `${(e.totalMinutes / maxMinutes) * 100}%`,
                              }}
                            />
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </GlassPanel>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
