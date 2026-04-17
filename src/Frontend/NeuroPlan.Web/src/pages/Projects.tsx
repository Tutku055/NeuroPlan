import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import {
  Plus,
  Pencil,
  Trash2,
  Printer,
  RefreshCw,
  ChevronRight,
  Activity,
  FolderOpen,
  Sparkles,
  Calendar,
  Hash,
  AlertTriangle,
} from "lucide-react";
import { GlassPanel } from "../components/UI/GlassPanel";
import { Button } from "../components/UI/Button";
import { Modal } from "../components/UI/Modal";
import { InputField } from "../components/UI/InputField";
import { useToast } from "../components/UI/Toast";
import { api } from "../api/client";

interface Project {
  id: string;
  name: string;
  projectCode: string;
  description: string;
  targetEndDate: string | null;
}

const EMPTY_FORM = {
  name: "",
  projectCode: "",
  description: "",
  targetEndDate: "",
};

/* ────────────────────────── Forecast renderer ── */
const ForecastDisplay: React.FC<{ data: any }> = ({ data }) => {
  if (data.error) {
    return (
      <div className="forecast-block error animate-fade-y">
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "0.5rem",
            marginBottom: "0.5rem",
          }}
        >
          <AlertTriangle size={15} color="#fb7185" />
          <span
            style={{ fontSize: "0.85rem", fontWeight: 600, color: "#fb7185" }}
          >
            Forecast Error
          </span>
        </div>
        <p style={{ fontSize: "0.83rem", color: "#fda4af", margin: 0 }}>
          {data.error}
        </p>
      </div>
    );
  }

  const rows: [string, string][] = Object.entries(data).map(([k, v]) => [
    k.replace(/([A-Z])/g, " $1").trim(),
    typeof v === "object" ? JSON.stringify(v) : String(v),
  ]);

  return (
    <div className="forecast-block animate-fade-y">
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: "0.5rem",
          marginBottom: "0.75rem",
        }}
      >
        <Sparkles size={15} color="var(--accent-secondary)" />
        <span
          style={{
            fontSize: "0.85rem",
            fontWeight: 600,
            color: "var(--accent-secondary)",
          }}
        >
          AI Forecast
        </span>
      </div>
      {rows.map(([label, value]) => (
        <div className="forecast-row" key={label}>
          <span className="label">{label}</span>
          <span className="value">{value}</span>
        </div>
      ))}
    </div>
  );
};

/* ────────────────────────── Main component ── */
export const Projects: React.FC = () => {
  const { permissions } = useAuth();
  const navigate = useNavigate();
  const { toast, confirm } = useToast();

  const canManage =
    permissions.includes("ManageProjects") ||
    permissions.includes("CreateProject");
  const canForecast = permissions.includes("AssessRisk");

  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedProject, setSelected] = useState<Project | null>(null);

  const [showForm, setShowForm] = useState(false);
  const [editTarget, setEditTarget] = useState<Project | null>(null);
  const [form, setForm] = useState({ ...EMPTY_FORM });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const [forecast, setForecast] = useState<any>(null);
  const [forecastLoading, setForecastLoading] = useState(false);

  const fetchProjects = async () => {
    setLoading(true);
    try {
      const res = await api.get("/projects");
      setProjects(res.data);
    } catch {
      /* ignore */
    }
    setLoading(false);
  };

  useEffect(() => {
    fetchProjects();
  }, []);

  const openCreate = () => {
    setEditTarget(null);
    setForm({ ...EMPTY_FORM });
    setFormError("");
    setShowForm(true);
  };

  const openEdit = (p: Project) => {
    setEditTarget(p);
    setForm({
      name: p.name,
      projectCode: p.projectCode,
      description: p.description,
      targetEndDate: p.targetEndDate ? p.targetEndDate.substring(0, 10) : "",
    });
    setFormError("");
    setShowForm(true);
  };

  const handleDelete = async (p: Project) => {
    const ok = await confirm(
      `Delete "${p.name}"?`,
      "This action cannot be undone. All tasks in this project will also be removed.",
    );
    if (!ok) return;
    try {
      await api.delete(`/projects/${p.id}`);
      if (selectedProject?.id === p.id) setSelected(null);
      toast("success", "Project deleted", `"${p.name}" was removed.`);
      fetchProjects();
    } catch (e: any) {
      toast(
        "error",
        "Delete failed",
        e.response?.data?.message || "An error occurred.",
      );
    }
  };

  const handleSave = async () => {
    if (!form.name.trim()) {
      setFormError("Name is required.");
      return;
    }
    setSaving(true);
    setFormError("");
    try {
      const payload = {
        ...(editTarget
          ? { id: editTarget.id }
          : { id: "00000000-0000-0000-0000-000000000000" }),
        name: form.name,
        projectCode: form.projectCode,
        description: form.description,
        targetEndDate: form.targetEndDate || null,
      };
      if (editTarget) {
        await api.put(`/projects/${editTarget.id}`, payload);
        toast("success", "Project updated", `"${form.name}" was saved.`);
      } else {
        await api.post("/projects", payload);
        toast(
          "success",
          "Project created",
          `"${form.name}" was added successfully.`,
        );
      }
      setShowForm(false);
      fetchProjects();
    } catch (e: any) {
      setFormError(e.response?.data?.message || "Save failed.");
    }
    setSaving(false);
  };

  const handleForecast = async (p: Project) => {
    setForecastLoading(true);
    setForecast(null);
    try {
      const res = await api.get(`/projects/${p.id}/forecast`);
      setForecast(res.data);
    } catch (e: any) {
      setForecast({ error: e.response?.data?.message || "Forecast failed." });
    }
    setForecastLoading(false);
  };

  /* ── Render ── */
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
      <div className="page-header no-print">
        <div>
          <h1 style={{ marginBottom: "0.2rem" }}>Projects</h1>
          <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
            Manage environments and trigger AI forecasts.
          </p>
        </div>
        <div className="page-header-actions">
          <Button
            variant="secondary"
            onClick={fetchProjects}
            icon={
              <RefreshCw size={15} className={loading ? "animate-spin" : ""} />
            }
          >
            Refresh
          </Button>
          {canManage && (
            <Button onClick={openCreate} icon={<Plus size={15} />}>
              New Project
            </Button>
          )}
        </div>
      </div>

      {/* Two-column body */}
      <div style={{ display: "flex", gap: "1.5rem", flex: 1, minHeight: 0 }}>
        {/* ── Left: list ── */}
        <div
          className="no-print"
          style={{
            width: 340,
            flexShrink: 0,
            display: "flex",
            flexDirection: "column",
            overflow: "hidden",
          }}
        >
          <GlassPanel
            style={{
              flex: 1,
              display: "flex",
              flexDirection: "column",
              gap: "0.5rem",
              overflow: "hidden",
              padding: "1rem",
            }}
          >
            <p
              style={{
                fontSize: "0.72rem",
                fontWeight: 600,
                color: "var(--text-muted)",
                textTransform: "uppercase",
                letterSpacing: "0.07em",
                marginBottom: "0.25rem",
              }}
            >
              {projects.length} Project{projects.length !== 1 ? "s" : ""}
            </p>
            <div
              style={{
                flex: 1,
                overflowY: "auto",
                display: "flex",
                flexDirection: "column",
                gap: "0.4rem",
              }}
            >
              {loading && (
                <>
                  {[1, 2, 3].map((i) => (
                    <div
                      key={i}
                      className="skeleton"
                      style={{ height: 68, borderRadius: 12 }}
                    />
                  ))}
                </>
              )}
              {!loading && projects.length === 0 && (
                <div
                  style={{
                    textAlign: "center",
                    padding: "3rem 1rem",
                    color: "var(--text-muted)",
                  }}
                >
                  <FolderOpen
                    size={32}
                    style={{ opacity: 0.3, marginBottom: "0.75rem" }}
                  />
                  <p style={{ fontSize: "0.875rem" }}>
                    No projects yet.
                    {canManage && ' Click "New Project" to create one.'}
                  </p>
                </div>
              )}
              {projects.map((p) => {
                const active = selectedProject?.id === p.id;
                return (
                  <div
                    key={p.id}
                    onClick={() => {
                      setSelected(p);
                      setForecast(null);
                    }}
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "0.75rem",
                      padding: "0.75rem 0.9rem",
                      background: active
                        ? "rgba(124,58,237,0.15)"
                        : "rgba(0,0,0,0.15)",
                      border: `1px solid ${active ? "rgba(124,58,237,0.4)" : "transparent"}`,
                      borderRadius: 12,
                      cursor: "pointer",
                      transition: "all 0.18s ease",
                    }}
                    onMouseEnter={(e) => {
                      if (!active)
                        (e.currentTarget as HTMLDivElement).style.background =
                          "rgba(255,255,255,0.04)";
                    }}
                    onMouseLeave={(e) => {
                      if (!active)
                        (e.currentTarget as HTMLDivElement).style.background =
                          "rgba(0,0,0,0.15)";
                    }}
                  >
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div
                        style={{
                          display: "flex",
                          justifyContent: "space-between",
                          gap: "0.5rem",
                          alignItems: "center",
                        }}
                      >
                        <span
                          style={{
                            fontWeight: 500,
                            fontSize: "0.875rem",
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap",
                          }}
                        >
                          {p.name}
                        </span>
                        {p.projectCode && (
                          <span className="tag">{p.projectCode}</span>
                        )}
                      </div>
                      {p.description && (
                        <p
                          style={{
                            color: "var(--text-muted)",
                            fontSize: "0.76rem",
                            margin: "0.2rem 0 0",
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap",
                          }}
                        >
                          {p.description}
                        </p>
                      )}
                    </div>
                    {canManage && (
                      <div
                        style={{
                          display: "flex",
                          gap: "0.25rem",
                          flexShrink: 0,
                        }}
                        onClick={(e) => e.stopPropagation()}
                      >
                        <button
                          onClick={() => openEdit(p)}
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
                          onClick={() => handleDelete(p)}
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
                );
              })}
            </div>
          </GlassPanel>
        </div>

        {/* ── Right: detail ── */}
        <div
          style={{
            flex: 1,
            display: "flex",
            flexDirection: "column",
            overflow: "hidden",
            minWidth: 0,
          }}
        >
          {selectedProject ? (
            <GlassPanel
              className="print-area"
              style={{
                flex: 1,
                display: "flex",
                flexDirection: "column",
                overflow: "hidden",
                padding: "1.5rem",
              }}
            >
              {/* panel header */}
              <div
                className="no-print"
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "flex-start",
                  flexWrap: "wrap",
                  gap: "0.75rem",
                  marginBottom: "1.25rem",
                }}
              >
                <div>
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "0.6rem",
                      marginBottom: "0.3rem",
                    }}
                  >
                    <h2 style={{ fontSize: "1.25rem", margin: 0 }}>
                      {selectedProject.name}
                    </h2>
                    <span className="tag">
                      {selectedProject.projectCode || "—"}
                    </span>
                  </div>
                </div>
                <div
                  style={{
                    display: "flex",
                    gap: "0.5rem",
                    flexWrap: "wrap",
                    alignItems: "center",
                  }}
                >
                  {canForecast && (
                    <Button
                      variant="secondary"
                      onClick={() => handleForecast(selectedProject)}
                      icon={
                        <Activity
                          size={14}
                          className={forecastLoading ? "animate-pulse" : ""}
                        />
                      }
                      style={{
                        fontSize: "0.82rem",
                        padding: "0.45rem 0.85rem",
                      }}
                    >
                      {forecastLoading ? "Analysing…" : "AI Forecast"}
                    </Button>
                  )}
                  <Button
                    onClick={() =>
                      navigate(`/projects/${selectedProject.id}/tasks`)
                    }
                    icon={<ChevronRight size={14} />}
                    style={{ fontSize: "0.82rem", padding: "0.45rem 0.85rem" }}
                  >
                    Tasks
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => window.print()}
                    icon={<Printer size={14} />}
                    style={{ fontSize: "0.82rem", padding: "0.45rem 0.85rem" }}
                  >
                    Print QR
                  </Button>
                </div>
              </div>

              {/* Description */}
              {selectedProject.description && (
                <p
                  className="no-print"
                  style={{
                    color: "var(--text-secondary)",
                    fontSize: "0.875rem",
                    marginBottom: "1rem",
                    lineHeight: 1.6,
                  }}
                >
                  {selectedProject.description}
                </p>
              )}

              {/* Meta row */}
              <div
                className="no-print"
                style={{
                  display: "flex",
                  gap: "1.25rem",
                  marginBottom: "1.25rem",
                  flexWrap: "wrap",
                }}
              >
                {selectedProject.targetEndDate && (
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "0.4rem",
                      fontSize: "0.8rem",
                      color: "var(--text-muted)",
                    }}
                  >
                    <Calendar size={13} />
                    <span>
                      Due:{" "}
                      <strong style={{ color: "var(--text-secondary)" }}>
                        {new Date(
                          selectedProject.targetEndDate,
                        ).toLocaleDateString("en-GB", {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                        })}
                      </strong>
                    </span>
                  </div>
                )}
                {selectedProject.projectCode && (
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "0.4rem",
                      fontSize: "0.8rem",
                      color: "var(--text-muted)",
                    }}
                  >
                    <Hash size={13} />
                    <span>
                      Code:{" "}
                      <strong style={{ color: "var(--text-secondary)" }}>
                        {selectedProject.projectCode}
                      </strong>
                    </span>
                  </div>
                )}
              </div>

              {/* Scrollable body */}
              <div
                style={{
                  flex: 1,
                  overflowY: "auto",
                  display: "flex",
                  flexDirection: "column",
                  gap: "1rem",
                }}
              >
                {/* QR block */}
                <div
                  style={{
                    textAlign: "center",
                    padding: "1.5rem",
                    background: "rgba(0,0,0,0.2)",
                    border: "1px solid var(--glass-border)",
                    borderRadius: "var(--radius-lg)",
                  }}
                >
                  <p
                    style={{
                      fontSize: "0.78rem",
                      color: "var(--text-muted)",
                      marginBottom: "0.9rem",
                      textTransform: "uppercase",
                      letterSpacing: "0.06em",
                    }}
                  >
                    Project QR Code
                  </p>
                  <div
                    style={{
                      background: "#fff",
                      padding: "0.8rem",
                      borderRadius: 12,
                      display: "inline-block",
                      boxShadow: "0 8px 30px rgba(0,0,0,0.5)",
                    }}
                  >
                    <img
                      src={`http://127.0.0.1:5000/api/projects/${selectedProject.projectCode || selectedProject.id}/qr`}
                      alt="QR Code"
                      style={{ width: 180, height: 180, display: "block" }}
                      crossOrigin="anonymous"
                    />
                  </div>
                  <p
                    className="no-print"
                    style={{
                      marginTop: "0.75rem",
                      fontSize: "0.77rem",
                      color: "var(--text-muted)",
                    }}
                  >
                    Scan to link work directly to this project via NeuroPlan
                    mobile.
                  </p>
                </div>

                {/* Forecast result */}
                {(forecastLoading || forecast) && (
                  <div>
                    {forecastLoading && (
                      <div
                        style={{
                          display: "flex",
                          flexDirection: "column",
                          gap: "0.5rem",
                        }}
                      >
                        {[1, 2, 3].map((i) => (
                          <div
                            key={i}
                            className="skeleton"
                            style={{ height: 36, borderRadius: 8 }}
                          />
                        ))}
                      </div>
                    )}
                    {forecast && !forecastLoading && (
                      <ForecastDisplay data={forecast} />
                    )}
                  </div>
                )}
              </div>
            </GlassPanel>
          ) : (
            <div
              style={{
                flex: 1,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                color: "var(--text-muted)",
                gap: "0.75rem",
                background: "rgba(255,255,255,0.015)",
                border: "1px dashed var(--glass-border)",
                borderRadius: "var(--radius-xl)",
              }}
            >
              <FolderOpen size={36} style={{ opacity: 0.25 }} />
              <p style={{ fontSize: "0.9rem" }}>
                Select a project to view details
              </p>
            </div>
          )}
        </div>
      </div>

      {/* ── Modal ── */}
      {showForm && (
        <Modal
          title={editTarget ? "Edit Project" : "New Project"}
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
            <FormField label="Project Name *">
              <InputField
                placeholder="e.g. NeuroPlan Backend"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </FormField>
            <FormField label="Project Code">
              <InputField
                placeholder="e.g. NP-001"
                value={form.projectCode}
                onChange={(e) =>
                  setForm({ ...form, projectCode: e.target.value })
                }
              />
            </FormField>
            <FormField label="Description">
              <textarea
                placeholder="Short description…"
                value={form.description}
                onChange={(e) =>
                  setForm({ ...form, description: e.target.value })
                }
                className="input-field"
                rows={3}
                style={{ resize: "vertical", lineHeight: 1.6 }}
              />
            </FormField>
            <FormField label="Target End Date">
              <InputField
                type="date"
                value={form.targetEndDate}
                onChange={(e) =>
                  setForm({ ...form, targetEndDate: e.target.value })
                }
              />
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
              <Button onClick={handleSave}>
                {saving
                  ? "Saving…"
                  : editTarget
                    ? "Update Project"
                    : "Create Project"}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};

/* Helper */
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
