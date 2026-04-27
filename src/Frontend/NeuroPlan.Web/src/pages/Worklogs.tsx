import React, { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { Play, Square, RefreshCw, ClipboardList, Layers } from "lucide-react";
import { GlassPanel } from "../components/UI/GlassPanel";
import { Button } from "../components/UI/Button";
import { useToast } from "../components/UI/Toast";
import { api } from "../api/client";

interface TaskItem {
  id: string;
  taskCode: string;
  title: string;
  description: string;
  complexityScore: number;
  status: number;
  projectId: string;
}

const STATUS_LABELS: Record<number, string> = {
  1: "Planned",
  2: "In Progress",
  3: "Cancelled",
  4: "Completed",
};
const STATUS_COLORS: Record<number, string> = {
  1: "#8b8bff",
  2: "#0ea5e9",
  3: "#ef4444",
  4: "#22c55e",
};
const STATUS_BG: Record<number, string> = {
  1: "rgba(139,139,255,0.1)",
  2: "rgba(14,165,233,0.1)",
  3: "rgba(239,68,68,0.1)",
  4: "rgba(34,197,94,0.1)",
};

const COMPLEXITY_COLOR = (n: number) => {
  if (n <= 3) return "#22c55e";
  if (n <= 6) return "#f59e0b";
  return "#ef4444";
};

export const Worklogs: React.FC = () => {
  const [searchParams] = useSearchParams();
  const projectCodeFilter = searchParams.get("projectCode");

  const { permissions, userId } = useAuth();
  const { toast } = useToast();

  const canTrack = permissions.includes("TrackWork");

  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [projects, setProjects] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  const fetchTasks = async () => {
    setLoading(true);
    try {
      const [projRes, taskRes] = await Promise.all([
        api.get("/projects").catch(() => ({ data: [] })),
        api.get("/tasks"),
      ]);

      const projMap: Record<string, string> = {};
      projRes.data.forEach((p: any) => {
        projMap[p.id] = p.name;
      });
      setProjects(projMap);

      let all: TaskItem[] = taskRes.data;
      if (projectCodeFilter) {
        // As our API doesn't currently expand the project in tasks fetch,
        // we match taskCode prefix if it follows PATTERN-XYZ as a fallback,
        // ideally the backend filters this but for now we show all if no API support:
        // We will fetch and show all tasks for simplistic operation if it's not possible to filter locally correctly yet.
      }

      // Restrict tasks that aren't "In Progress" from showing
      all = all.filter((t) => t.status === 2);

      setTasks(all);
    } catch {
      /* ignore */
    }
    setLoading(false);
  };

  useEffect(() => {
    fetchTasks();
  }, [projectCodeFilter]);

  const handleStart = async (t: TaskItem) => {
    if (!userId) {
      toast("error", "Unauthorized", "Please log in again.");
      return;
    }
    try {
      await api.post(`/worklogs/start?taskCode=${t.taskCode}`);
      toast("success", "Work log started", `Tracking time for "${t.title}".`);
      fetchTasks();
    } catch (e: any) {
      toast(
        "error",
        "Could not start log",
        e.response?.data?.message || "An error occurred.",
      );
    }
  };

  const handleStop = async (t: TaskItem) => {
    if (!userId) {
      toast("error", "Unauthorized", "Please log in again.");
      return;
    }
    try {
      await api.post(`/worklogs/stop?taskCode=${t.taskCode}&isCompleted=false`);
      toast("info", "Work log paused", `Timer paused for "${t.title}".`);
      fetchTasks();
    } catch (e: any) {
      toast(
        "error",
        "Could not stop log",
        e.response?.data?.message || "An error occurred.",
      );
    }
  };

  if (!canTrack) {
    return (
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          height: "100%",
          color: "var(--text-muted)",
        }}
      >
        Access Denied. You do not have permission to track work.
      </div>
    );
  }

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        overflow: "hidden",
      }}
    >
      {/* Header */}
      <div className="page-header">
        <div>
          <h1 style={{ marginBottom: "0.15rem" }}>Worklogs</h1>
          <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
            {projectCodeFilter
              ? `Log work for project ${projectCodeFilter}.`
              : "Track and log your active work progress."}
          </p>
        </div>
        <div className="page-header-actions">
          <Button
            variant="secondary"
            onClick={fetchTasks}
            icon={
              <RefreshCw size={15} className={loading ? "animate-spin" : ""} />
            }
          >
            Refresh
          </Button>
        </div>
      </div>

      {/* Grid */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {loading && (
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
              gap: "1rem",
            }}
          >
            {[1, 2, 3, 4].map((i) => (
              <div
                key={i}
                className="skeleton"
                style={{ height: 180, borderRadius: 20 }}
              />
            ))}
          </div>
        )}

        {!loading && tasks.length === 0 && (
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
            <ClipboardList size={36} style={{ opacity: 0.25 }} />
            <p style={{ fontSize: "0.9rem" }}>No assignable tasks available.</p>
          </div>
        )}

        {!loading && tasks.length > 0 && (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              gap: "2rem",
              paddingBottom: "1.5rem",
            }}
          >
            {Object.entries(
              tasks.reduce(
                (acc, t) => {
                  const pId = t.projectId || "unassigned";
                  if (!acc[pId]) acc[pId] = [];
                  acc[pId].push(t);
                  return acc;
                },
                {} as Record<string, TaskItem[]>,
              ),
            ).map(([pId, groupTasks]) => (
              <div
                key={pId}
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "1rem",
                }}
              >
                <h2
                  style={{
                    fontSize: "1.1rem",
                    borderBottom: "1px solid var(--glass-border)",
                    paddingBottom: "0.5rem",
                  }}
                >
                  {projects[pId] ||
                    (pId === "unassigned"
                      ? "Unassigned Tasks"
                      : "Unknown Project")}
                </h2>
                <div
                  style={{
                    display: "grid",
                    gridTemplateColumns:
                      "repeat(auto-fill, minmax(280px, 1fr))",
                    gap: "1rem",
                  }}
                >
                  {groupTasks.map((t) => (
                    <GlassPanel
                      key={t.id}
                      style={{
                        display: "flex",
                        flexDirection: "column",
                        gap: "0.85rem",
                        padding: "1.25rem",
                      }}
                    >
                      {/* task header */}
                      <div
                        style={{
                          display: "flex",
                          justifyContent: "space-between",
                          alignItems: "flex-start",
                          gap: "0.5rem",
                        }}
                      >
                        <h3
                          style={{
                            fontSize: "0.95rem",
                            lineHeight: 1.4,
                            flex: 1,
                          }}
                        >
                          {t.title}
                        </h3>
                        {t.taskCode && (
                          <span className="tag">{t.taskCode}</span>
                        )}
                      </div>

                      {t.description && (
                        <p
                          style={{
                            color: "var(--text-muted)",
                            fontSize: "0.82rem",
                            lineHeight: 1.6,
                            flex: 1,
                            margin: 0,
                            display: "-webkit-box",
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: "vertical",
                            overflow: "hidden",
                          }}
                        >
                          {t.description}
                        </p>
                      )}

                      {/* meta */}
                      <div
                        style={{
                          display: "flex",
                          gap: "0.6rem",
                          alignItems: "center",
                          flexWrap: "wrap",
                        }}
                      >
                        <span
                          style={{
                            display: "inline-flex",
                            alignItems: "center",
                            gap: "0.35rem",
                            padding: "0.12rem 0.55rem",
                            background: STATUS_BG[t.status],
                            color: STATUS_COLORS[t.status],
                            borderRadius: 99,
                            fontSize: "0.7rem",
                            fontWeight: 600,
                          }}
                        >
                          <span
                            style={{
                              width: 4,
                              height: 4,
                              borderRadius: "50%",
                              background: STATUS_COLORS[t.status],
                              flexShrink: 0,
                            }}
                          />
                          {STATUS_LABELS[t.status] ?? "Unknown"}
                        </span>
                        <span
                          style={{
                            display: "inline-flex",
                            alignItems: "center",
                            gap: "0.35rem",
                            padding: "0.12rem 0.55rem",
                            background: `${COMPLEXITY_COLOR(t.complexityScore)}18`,
                            color: COMPLEXITY_COLOR(t.complexityScore),
                            borderRadius: 99,
                            fontSize: "0.7rem",
                            fontWeight: 600,
                            marginLeft: "auto",
                          }}
                        >
                          <Layers size={10} />
                          {t.complexityScore}/10
                        </span>
                      </div>

                      {/* divider */}
                      <div
                        style={{ height: 1, background: "var(--glass-border)" }}
                      />

                      {/* actions */}
                      <div
                        style={{
                          display: "flex",
                          gap: "0.4rem",
                          flexWrap: "wrap",
                          alignItems: "center",
                        }}
                      >
                        <button
                          onClick={() => handleStart(t)}
                          title="Start work log"
                          disabled={t.status !== 2}
                          style={{
                            flex: 1,
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            gap: "0.3rem",
                            padding: "0.45rem 0.5rem",
                            background:
                              t.status === 2
                                ? "rgba(34,197,94,0.1)"
                                : "rgba(128,128,128,0.1)",
                            border:
                              t.status === 2
                                ? "1px solid rgba(34,197,94,0.2)"
                                : "1px solid rgba(128,128,128,0.2)",
                            color: t.status === 2 ? "#22c55e" : "#888",
                            borderRadius: 8,
                            cursor: t.status === 2 ? "pointer" : "not-allowed",
                            fontSize: "0.78rem",
                            fontFamily: "var(--font-family)",
                            fontWeight: 500,
                            transition: "all 0.15s",
                            whiteSpace: "nowrap",
                            opacity: t.status === 2 ? 1 : 0.5,
                          }}
                        >
                          <Play size={12} /> Start
                        </button>
                        <button
                          onClick={() => handleStop(t)}
                          title="Stop work log"
                          disabled={t.status !== 2}
                          style={{
                            flex: 1,
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            gap: "0.3rem",
                            padding: "0.45rem 0.5rem",
                            background: "rgba(245,158,11,0.1)",
                            border: "1px solid rgba(245,158,11,0.2)",
                            color: "#f59e0b",
                            borderRadius: 8,
                            cursor: t.status === 2 ? "pointer" : "not-allowed",
                            fontSize: "0.78rem",
                            fontFamily: "var(--font-family)",
                            fontWeight: 500,
                            transition: "all 0.15s",
                            whiteSpace: "nowrap",
                            opacity: t.status === 2 ? 1 : 0.5,
                          }}
                        >
                          <Square size={12} /> Stop Work
                        </button>
                      </div>
                    </GlassPanel>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
