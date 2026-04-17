import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import {
  Plus,
  Pencil,
  Trash2,
  Play,
  Square,
  CheckCheck,
  XCircle,
  ArrowLeft,
  RefreshCw,
  ClipboardList,
  Layers,
} from "lucide-react";
import { GlassPanel } from "../components/UI/GlassPanel";
import { Button } from "../components/UI/Button";
import { Modal } from "../components/UI/Modal";
import { InputField } from "../components/UI/InputField";
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

const EMPTY_FORM = {
  taskCode: "",
  title: "",
  description: "",
  complexityScore: 1,
  status: 1,
};

const FormField: React.FC<{ label: string; children: React.ReactNode }> = ({
  label,
  children,
}) => (
  <div>
    <label
      style={{
        display: "block",
        fontSize: "0.78rem",
        fontWeight: 500,
        color: "var(--text-secondary)",
        marginBottom: "0.4rem",
      }}
    >
      {label}
    </label>
    {children}
  </div>
);

export const Tasks: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const { permissions } = useAuth();
  const { toast, confirm } = useToast();

  const canManage =
    permissions.includes("ManageTaskItems") ||
    permissions.includes("ManageProjects");

  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [projectsMap, setProjectsMap] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [editTarget, setEditTarget] = useState<TaskItem | null>(null);
  const [form, setForm] = useState({ ...EMPTY_FORM });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");
  const [projectName, setProjectName] = useState("");

  const fetchTasks = async () => {
    setLoading(true);
    try {
      if (projectId) {
        const projRes = await api.get(`/projects/${projectId}`);
        setProjectName(projRes.data.name);
      } else {
        const projRes = await api.get("/projects");
        const pMap: Record<string, string> = {};
        projRes.data.forEach((p: any) => {
          pMap[p.id] = p.name;
        });
        setProjectsMap(pMap);
      }

      const res = await api.get("/tasks");
      const all: TaskItem[] = res.data;
      setTasks(projectId ? all.filter((t) => t.projectId === projectId) : all);
    } catch {
      /* ignore */
    }
    setLoading(false);
  };

  useEffect(() => {
    fetchTasks();
  }, [projectId]);

  const openCreate = () => {
    setEditTarget(null);
    setForm({ ...EMPTY_FORM });
    setFormError("");
    setShowForm(true);
  };

  const openEdit = (t: TaskItem) => {
    setEditTarget(t);
    setForm({
      taskCode: t.taskCode,
      title: t.title,
      description: t.description,
      complexityScore: t.complexityScore,
      status: t.status,
    });
    setFormError("");
    setShowForm(true);
  };

  const handleDelete = async (t: TaskItem) => {
    const ok = await confirm(
      `Delete "${t.title}"?`,
      "Are you sure you want to delete this task? This action cannot be undone.",
    );
    if (!ok) return;

    try {
      await api.delete(`/tasks/${t.id}`);
      toast(
        "success",
        "Task deleted",
        `"${t.title}" was successfully deleted.`,
      );
      fetchTasks();
    } catch {
      toast("error", "Could not delete task", "An error occurred.");
    }
  };

  const updateTaskStatus = async (t: TaskItem, newStatus: number) => {
    try {
      await api.put(`/tasks/${t.id}`, { ...t, status: newStatus });
      toast(
        "success",
        "Status Updated",
        `Task marked as ${STATUS_LABELS[newStatus]}.`,
      );
      fetchTasks();
    } catch (e: any) {
      toast(
        "error",
        "Update failed",
        e.response?.data?.message || "Could not change status.",
      );
    }
  };

  const handleSave = async () => {
    if (!form.title.trim()) {
      setFormError("Title is required.");
      return;
    }

    // Require a project association
    const effectiveProjectId = projectId ?? editTarget?.projectId;
    if (!effectiveProjectId) {
      setFormError(
        "Tasks must be associated with a project. Please open tasks from a project.",
      );
      return;
    }

    setSaving(true);
    setFormError("");
    try {
      if (editTarget) {
        const payload = {
          id: editTarget.id,
          taskCode: form.taskCode,
          title: form.title,
          description: form.description,
          complexityScore: Number(form.complexityScore),
          status: Number(form.status),
        };
        await api.put(`/tasks/${editTarget.id}`, payload);
        toast("success", "Task updated", `"${form.title}" was updated.`);
      } else {
        const payload = {
          taskCode: form.taskCode,
          title: form.title,
          description: form.description,
          complexityScore: Number(form.complexityScore),
          status: Number(form.status),
          projectId: effectiveProjectId,
        };
        await api.post("/tasks", payload);
        toast(
          "success",
          "Task created",
          `"${form.title}" was added successfully.`,
        );
      }
      setShowForm(false);
      fetchTasks();
    } catch (e: any) {
      const msg =
        e.response?.data?.message ||
        e.response?.data?.Message ||
        "Save failed.";
      setFormError(msg);
    }
    setSaving(false);
  };

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
        <div style={{ display: "flex", gap: "1rem", alignItems: "center" }}>
          {projectId && (
            <button
              onClick={() => navigate("/projects")}
              style={{
                background: "rgba(255,255,255,0.05)",
                border: "1px solid var(--glass-border)",
                color: "var(--text-secondary)",
                cursor: "pointer",
                display: "flex",
                alignItems: "center",
                gap: "0.35rem",
                padding: "0.45rem 0.75rem",
                borderRadius: "var(--radius-md)",
                fontSize: "0.82rem",
                fontFamily: "var(--font-family)",
                transition: "all 0.2s",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background =
                  "rgba(255,255,255,0.09)";
                (e.currentTarget as HTMLButtonElement).style.color =
                  "var(--text-main)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background =
                  "rgba(255,255,255,0.05)";
                (e.currentTarget as HTMLButtonElement).style.color =
                  "var(--text-secondary)";
              }}
            >
              <ArrowLeft size={14} />
              Back to Projects
            </button>
          )}
          <div>
            <h1 style={{ marginBottom: "0.15rem" }}>
              {projectName ? `${projectName} â€“ Tasks` : "Tasks"}
            </h1>
            <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
              {projectName
                ? `Plan, track and log work across ${projectName}.`
                : "Plan, track and log work across all projects."}
            </p>
          </div>
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
          {canManage && (
            <Button onClick={openCreate} icon={<Plus size={15} />}>
              New Task
            </Button>
          )}
        </div>
      </div>

      {/* Stats row */}
      {!loading && tasks.length > 0 && (
        <div
          style={{
            display: "flex",
            gap: "0.75rem",
            marginBottom: "1.25rem",
            flexWrap: "wrap",
          }}
        >
          {(
            [
              {
                label: "Total",
                count: tasks.length,
                color: "var(--text-secondary)",
              },
              {
                label: "In Progress",
                count: tasks.filter((t) => t.status === 2).length,
                color: "#0ea5e9",
              },
              {
                label: "Completed",
                count: tasks.filter((t) => t.status === 4).length,
                color: "#22c55e",
              },
              {
                label: "Planned",
                count: tasks.filter((t) => t.status === 1).length,
                color: "#8b8bff",
              },
            ] as const
          ).map((s) => (
            <div
              key={s.label}
              style={{
                padding: "0.5rem 1rem",
                background: "var(--glass-bg)",
                border: "1px solid var(--glass-border)",
                borderRadius: "var(--radius-md)",
                display: "flex",
                gap: "0.5rem",
                alignItems: "center",
                fontSize: "0.8rem",
              }}
            >
              <span
                style={{ color: s.color, fontWeight: 700, fontSize: "1rem" }}
              >
                {s.count}
              </span>
              <span style={{ color: "var(--text-muted)" }}>{s.label}</span>
            </div>
          ))}
        </div>
      )}

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
            <p style={{ fontSize: "0.9rem" }}>
              No tasks yet.{canManage && ' Click "New Task" to add one.'}
            </p>
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
                  {projectName ||
                    projectsMap[pId] ||
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
                        {canManage && (
                          <div
                            style={{
                              display: "flex",
                              gap: "0.4rem",
                              width: "100%",
                            }}
                          >
                            {t.status !== 2 &&
                              t.status !== 4 &&
                              t.status !== 3 && (
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    updateTaskStatus(t, 2);
                                  }}
                                  title="Set to In Progress"
                                  style={{
                                    flex: 1,
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    gap: "0.2rem",
                                    padding: "0.45rem",
                                    background: "rgba(14,165,233,0.1)",
                                    border: "1px solid rgba(14,165,233,0.2)",
                                    color: "#0ea5e9",
                                    borderRadius: 8,
                                    cursor: "pointer",
                                    transition: "all 0.15s",
                                  }}
                                >
                                  <Play size={12} /> Start
                                </button>
                              )}

                            {t.status === 2 && (
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  updateTaskStatus(t, 1);
                                }}
                                title="Revert to Planned"
                                style={{
                                  flex: 1,
                                  display: "flex",
                                  alignItems: "center",
                                  justifyContent: "center",
                                  gap: "0.2rem",
                                  padding: "0.45rem",
                                  background: "rgba(139,139,255,0.1)",
                                  border: "1px solid rgba(139,139,255,0.2)",
                                  color: "#8b8bff",
                                  borderRadius: 8,
                                  cursor: "pointer",
                                  transition: "all 0.15s",
                                }}
                              >
                                <Square size={12} /> Pause
                              </button>
                            )}

                            {t.status !== 4 && (
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  updateTaskStatus(t, 4);
                                }}
                                title="Set to Done"
                                style={{
                                  flex: 1,
                                  display: "flex",
                                  alignItems: "center",
                                  justifyContent: "center",
                                  gap: "0.2rem",
                                  padding: "0.45rem",
                                  background: "rgba(34,197,94,0.1)",
                                  border: "1px solid rgba(34,197,94,0.2)",
                                  color: "#22c55e",
                                  borderRadius: 8,
                                  cursor: "pointer",
                                  transition: "all 0.15s",
                                }}
                              >
                                <CheckCheck size={12} /> Done
                              </button>
                            )}

                            {t.status !== 3 && t.status !== 4 && (
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  updateTaskStatus(t, 3);
                                }}
                                title="Cancel Task"
                                style={{
                                  flex: 1,
                                  display: "flex",
                                  alignItems: "center",
                                  justifyContent: "center",
                                  gap: "0.2rem",
                                  padding: "0.45rem",
                                  background: "rgba(239,68,68,0.1)",
                                  border: "1px solid rgba(239,68,68,0.2)",
                                  color: "#ef4444",
                                  borderRadius: 8,
                                  cursor: "pointer",
                                  transition: "all 0.15s",
                                }}
                              >
                                <XCircle size={12} /> Cancel
                              </button>
                            )}

                            <div style={{ flex: 1 }} />
                            <button
                              onClick={() => openEdit(t)}
                              title="Edit"
                              style={{
                                background: "none",
                                border: "none",
                                cursor: "pointer",
                                color: "var(--text-muted)",
                                padding: "0.3rem",
                                borderRadius: 6,
                                display: "flex",
                                transition: "color 0.15s",
                              }}
                              onMouseEnter={(e) =>
                                ((
                                  e.currentTarget as HTMLButtonElement
                                ).style.color = "#fff")
                              }
                              onMouseLeave={(e) =>
                                ((
                                  e.currentTarget as HTMLButtonElement
                                ).style.color = "var(--text-muted)")
                              }
                            >
                              <Pencil size={13} />
                            </button>
                            <button
                              onClick={() => handleDelete(t)}
                              title="Delete"
                              style={{
                                background: "none",
                                border: "none",
                                cursor: "pointer",
                                color: "var(--text-muted)",
                                padding: "0.3rem",
                                borderRadius: 6,
                                display: "flex",
                                transition: "color 0.15s",
                              }}
                              onMouseEnter={(e) =>
                                ((
                                  e.currentTarget as HTMLButtonElement
                                ).style.color = "#ef4444")
                              }
                              onMouseLeave={(e) =>
                                ((
                                  e.currentTarget as HTMLButtonElement
                                ).style.color = "var(--text-muted)")
                              }
                            >
                              <Trash2 size={13} />
                            </button>
                          </div>
                        )}
                      </div>
                    </GlassPanel>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Modal */}
      {/* Modal */}
      {showForm && (
        <Modal
          title={editTarget ? "Edit Task" : "New Task"}
          onClose={() => setShowForm(false)}
        >
          <div
            style={{ display: "flex", flexDirection: "column", gap: "1rem" }}
          >
            {formError && (
              <div
                style={{
                  padding: "0.65rem 0.9rem",
                  background: "rgba(244,63,94,0.1)",
                  border: "1px solid rgba(244,63,94,0.25)",
                  color: "#fb7185",
                  borderRadius: 8,
                  fontSize: "0.84rem",
                }}
              >
                {formError}
              </div>
            )}
            <FormField label="Task Code">
              <InputField
                placeholder="e.g. TASK-001"
                value={form.taskCode}
                onChange={(e) => setForm({ ...form, taskCode: e.target.value })}
              />
            </FormField>
            <FormField label="Title *">
              <InputField
                placeholder="Task titleâ€¦"
                value={form.title}
                onChange={(e) => setForm({ ...form, title: e.target.value })}
              />
            </FormField>
            <FormField label="Description">
              <textarea
                placeholder="Detailsâ€¦"
                value={form.description}
                onChange={(e) =>
                  setForm({ ...form, description: e.target.value })
                }
                className="input-field"
                rows={3}
                style={{ resize: "vertical", lineHeight: 1.6 }}
              />
            </FormField>
            <FormField label="Complexity (1 â€“ 10)">
              <InputField
                type="number"
                min="1"
                max="10"
                value={String(form.complexityScore)}
                onChange={(e) =>
                  setForm({ ...form, complexityScore: Number(e.target.value) })
                }
              />
            </FormField>
            <FormField label="Status">
              <select
                value={form.status}
                onChange={(e) =>
                  setForm({ ...form, status: Number(e.target.value) })
                }
                className="input-field"
              >
                {Object.entries(STATUS_LABELS).map(([k, v]) => (
                  <option key={k} value={k}>
                    {v}
                  </option>
                ))}
              </select>
            </FormField>
            <div
              style={{
                display: "flex",
                gap: "0.75rem",
                justifyContent: "flex-end",
                marginTop: "0.25rem",
                paddingTop: "0.75rem",
                borderTop: "1px solid var(--glass-border)",
              }}
            >
              <Button variant="secondary" onClick={() => setShowForm(false)}>
                Cancel
              </Button>
              <Button onClick={handleSave} disabled={saving}>
                {saving
                  ? "Savingâ€¦"
                  : editTarget
                    ? "Update Task"
                    : "Create Task"}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
