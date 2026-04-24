import React, { useEffect, useState } from "react";
import { Plus, Pencil, Trash2, RefreshCw, Shield } from "lucide-react";
import { GlassPanel } from "../components/UI/GlassPanel";
import { Button } from "../components/UI/Button";
import { Modal } from "../components/UI/Modal";
import { InputField } from "../components/UI/InputField";
import { useToast } from "../components/UI/Toast";
import { api } from "../api/client";

interface RoleItem {
  id: string;
  name: string;
  color?: string;
  permissionIds: string[];
}

interface PermissionItem {
  id: string;
  systemName: string;
  label?: string;
  description: string;
}

const DEFAULT_ROLE_COLOR = "#64748B";

const EMPTY_FORM = {
  name: "",
  color: DEFAULT_ROLE_COLOR,
  permissionIds: [] as string[],
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

export const Roles: React.FC = () => {
  const { toast, confirm } = useToast();

  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [permissions, setPermissions] = useState<PermissionItem[]>([]);
  const [loading, setLoading] = useState(false);

  const [showForm, setShowForm] = useState(false);
  const [editTarget, setEditTarget] = useState<RoleItem | null>(null);
  const [form, setForm] = useState({ ...EMPTY_FORM });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const fetchData = async () => {
    setLoading(true);
    try {
      const [rolesRes, permsRes] = await Promise.all([
        api.get("/roles"),
        api.get("/permissions"),
      ]);
      setRoles(rolesRes.data);
      setPermissions(permsRes.data);
    } catch {}
    setLoading(false);
  };

  useEffect(() => {
    fetchData();
  }, []);

  const openCreate = () => {
    setEditTarget(null);
    setForm({ ...EMPTY_FORM, color: DEFAULT_ROLE_COLOR, permissionIds: [] });
    setFormError("");
    setShowForm(true);
  };

  const openEdit = (r: RoleItem) => {
    setEditTarget(r);
    setForm({
      name: r.name,
      color: normalizeHexColor(r.color || DEFAULT_ROLE_COLOR),
      permissionIds: [...r.permissionIds],
    });
    setFormError("");
    setShowForm(true);
  };

  const handleDelete = async (r: RoleItem) => {
    const ok = await confirm(
      `Delete "${r.name}"?`,
      "This role will be removed. Users assigned to it must be reassigned first.",
    );
    if (!ok) return;
    try {
      await api.delete(`/roles/${r.id}`);
      toast("success", "Role deleted", `"${r.name}" was removed.`);
      fetchData();
    } catch (e: any) {
      if (e.response?.status === 409) {
        toast(
          "warning",
          "Role is in use",
          e.response?.data?.message ||
            "Reassign active users before deleting this role.",
        );
        return;
      }

      toast(
        "error",
        "Delete failed",
        e.response?.data?.message || "An error occurred.",
      );
    }
  };

  const togglePermission = (permId: string) => {
    setForm((prev) => {
      const exists = prev.permissionIds.includes(permId);
      return {
        ...prev,
        permissionIds: exists
          ? prev.permissionIds.filter((id) => id !== permId)
          : [...prev.permissionIds, permId],
      };
    });
  };

  const handleSave = async () => {
    if (!form.name.trim()) {
      setFormError("Name is required.");
      return;
    }

    if (!isValidHexColor(form.color)) {
      setFormError("Color must be a valid hex code like #3B82F6.");
      return;
    }

    const normalizedColor = normalizeHexColor(form.color);

    setSaving(true);
    setFormError("");
    try {
      if (editTarget) {
        await api.put(`/roles/${editTarget.id}`, {
          name: form.name,
          color: normalizedColor,
          permissionIds: form.permissionIds,
        });
        toast("success", "Role updated", `"${form.name}" was saved.`);
      } else {
        await api.post("/roles", {
          name: form.name,
          color: normalizedColor,
          permissionIds: form.permissionIds,
        });
        toast(
          "success",
          "Role created",
          `"${form.name}" was added successfully.`,
        );
      }
      setShowForm(false);
      fetchData();
    } catch (e: any) {
      setFormError(e.response?.data?.message || "Save failed.");
    }
    setSaving(false);
  };

  const getPermissionName = (id: string) => {
    const p = permissions.find((p) => p.id === id);
    return p?.label || p?.description || p?.systemName || id;
  };

  const normalizeHexColor = (value: string) => value.trim().toUpperCase();

  const isValidHexColor = (value: string) =>
    /^#[0-9A-F]{6}$/.test(normalizeHexColor(value));

  const roleColorValue = (color?: string) =>
    isValidHexColor(color || "")
      ? normalizeHexColor(color || "")
      : DEFAULT_ROLE_COLOR;

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
          <h1 style={{ marginBottom: "0.15rem" }}>Roles</h1>
          <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
            Manage roles and assign permissions.
          </p>
        </div>
        <div className="page-header-actions">
          <Button
            variant="secondary"
            onClick={fetchData}
            icon={
              <RefreshCw size={15} className={loading ? "animate-spin" : ""} />
            }
          >
            Refresh
          </Button>
          <Button onClick={openCreate} icon={<Plus size={15} />}>
            New Role
          </Button>
        </div>
      </div>

      <div style={{ flex: 1, overflowY: "auto" }}>
        {loading && (
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
                style={{ height: 52, borderRadius: 12 }}
              />
            ))}
          </div>
        )}

        {!loading && roles.length === 0 && (
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
            <Shield size={36} style={{ opacity: 0.25 }} />
            <p style={{ fontSize: "0.9rem" }}>No roles found.</p>
          </div>
        )}

        {!loading && roles.length > 0 && (
          <GlassPanel style={{ padding: "0", overflow: "hidden" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Role Name</th>
                  <th>Color</th>
                  <th>Permissions</th>
                  <th style={{ width: 100, textAlign: "right" }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {roles.map((r) => {
                  const currentColor = roleColorValue(r.color);
                  return (
                    <tr key={r.id}>
                      <td>
                        <div
                          style={{
                            display: "flex",
                            alignItems: "center",
                            gap: "0.5rem",
                          }}
                        >
                          <Shield size={14} color={currentColor} />
                          <span style={{ fontWeight: 500 }}>{r.name}</span>
                        </div>
                      </td>
                      <td>
                        <div
                          style={{
                            display: "inline-flex",
                            alignItems: "center",
                            gap: "0.45rem",
                            fontSize: "0.76rem",
                            color: "var(--text-secondary)",
                          }}
                        >
                          <span
                            style={{
                              width: 12,
                              height: 12,
                              borderRadius: "50%",
                              background: currentColor,
                              border: "1px solid rgba(255,255,255,0.25)",
                              display: "inline-block",
                            }}
                          />
                          {currentColor}
                        </div>
                      </td>
                      <td>
                        <div
                          style={{
                            display: "flex",
                            gap: "0.3rem",
                            flexWrap: "wrap",
                          }}
                        >
                          {r.permissionIds.length === 0 && (
                            <span
                              style={{
                                color: "var(--text-muted)",
                                fontSize: "0.78rem",
                              }}
                            >
                              No permissions
                            </span>
                          )}
                          {r.permissionIds.map((pid) => (
                            <span
                              key={pid}
                              className="tag"
                              style={{ fontSize: "0.7rem" }}
                              title={
                                permissions.find((p) => p.id === pid)
                                  ?.systemName || pid
                              }
                            >
                              {getPermissionName(pid)}
                            </span>
                          ))}
                        </div>
                      </td>
                      <td>
                        <div
                          style={{
                            display: "flex",
                            gap: "0.25rem",
                            justifyContent: "flex-end",
                          }}
                        >
                          <button
                            onClick={() => openEdit(r)}
                            title="Edit"
                            style={{
                              background: "none",
                              border: "none",
                              cursor: "pointer",
                              color: "var(--text-muted)",
                              padding: "0.35rem",
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
                            <Pencil size={14} />
                          </button>
                          <button
                            onClick={() => handleDelete(r)}
                            title="Delete"
                            style={{
                              background: "none",
                              border: "none",
                              cursor: "pointer",
                              color: "var(--text-muted)",
                              padding: "0.35rem",
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
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </GlassPanel>
        )}
      </div>

      {showForm && (
        <Modal
          title={editTarget ? "Edit Role" : "New Role"}
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
            <FormField label="Role Name *">
              <InputField
                placeholder="e.g. Supervisor"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </FormField>
            <FormField label="Role Color *">
              <div
                style={{
                  display: "flex",
                  gap: "0.65rem",
                  alignItems: "center",
                }}
              >
                <input
                  type="color"
                  value={roleColorValue(form.color)}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      color: normalizeHexColor(e.target.value),
                    })
                  }
                  style={{
                    width: 40,
                    height: 34,
                    border: "1px solid var(--glass-border)",
                    borderRadius: 8,
                    background: "transparent",
                    padding: 2,
                    cursor: "pointer",
                  }}
                />
                <InputField
                  placeholder="#64748B"
                  value={form.color}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      color: normalizeHexColor(e.target.value),
                    })
                  }
                />
              </div>
            </FormField>
            <FormField label="Permissions">
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: "0.15rem",
                  maxHeight: 260,
                  overflowY: "auto",
                  padding: "0.5rem 0.75rem",
                  background: "rgba(0,0,0,0.2)",
                  border: "1px solid var(--glass-border)",
                  borderRadius: "var(--radius-md)",
                }}
              >
                {permissions.map((p) => (
                  <label key={p.id} className="checkbox-label">
                    <input
                      type="checkbox"
                      checked={form.permissionIds.includes(p.id)}
                      onChange={() => togglePermission(p.id)}
                    />
                    <div>
                      <div style={{ fontWeight: 500 }}>
                        {p.label || p.description || p.systemName}
                      </div>
                      {p.description && (
                        <div
                          style={{
                            fontSize: "0.72rem",
                            color: "var(--text-muted)",
                            marginTop: "0.1rem",
                          }}
                        >
                          {p.systemName}
                        </div>
                      )}
                    </div>
                  </label>
                ))}
                {permissions.length === 0 && (
                  <span
                    style={{
                      color: "var(--text-muted)",
                      fontSize: "0.82rem",
                      padding: "0.5rem 0",
                    }}
                  >
                    No permissions available.
                  </span>
                )}
              </div>
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
                    ? "Update Role"
                    : "Create Role"}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
