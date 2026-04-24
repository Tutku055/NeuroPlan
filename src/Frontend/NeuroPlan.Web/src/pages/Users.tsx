import React, { useEffect, useState } from "react";
import {
  Plus,
  Pencil,
  Trash2,
  RefreshCw,
  Users as UsersIcon,
} from "lucide-react";
import { GlassPanel } from "../components/UI/GlassPanel";
import { Button } from "../components/UI/Button";
import { Modal } from "../components/UI/Modal";
import { InputField } from "../components/UI/InputField";
import { useToast } from "../components/UI/Toast";
import { api } from "../api/client";

interface UserItem {
  id: string;
  fullName: string;
  email: string;
  roleId: string;
  roleName: string;
}

interface RoleOption {
  id: string;
  name: string;
}

const EMPTY_FORM = {
  fullName: "",
  email: "",
  password: "",
  roleId: "",
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

export const Users: React.FC = () => {
  const { toast, confirm } = useToast();

  const [users, setUsers] = useState<UserItem[]>([]);
  const [roles, setRoles] = useState<RoleOption[]>([]);
  const [loading, setLoading] = useState(false);

  const [showForm, setShowForm] = useState(false);
  const [editTarget, setEditTarget] = useState<UserItem | null>(null);
  const [form, setForm] = useState({ ...EMPTY_FORM });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const fetchData = async () => {
    setLoading(true);
    try {
      const [usersRes, rolesRes] = await Promise.all([
        api.get("/users"),
        api.get("/roles"),
      ]);
      setUsers(usersRes.data);
      setRoles(
        rolesRes.data.map((r: any) => ({ id: r.id, name: r.name })),
      );
    } catch {}
    setLoading(false);
  };

  useEffect(() => {
    fetchData();
  }, []);

  const openCreate = () => {
    setEditTarget(null);
    setForm({ ...EMPTY_FORM });
    setFormError("");
    setShowForm(true);
  };

  const openEdit = (u: UserItem) => {
    setEditTarget(u);
    setForm({
      fullName: u.fullName,
      email: u.email,
      password: "",
      roleId: u.roleId,
    });
    setFormError("");
    setShowForm(true);
  };

  const handleDelete = async (u: UserItem) => {
    const ok = await confirm(
      `Delete "${u.fullName}"?`,
      "This user will be soft-deleted and can no longer log in.",
    );
    if (!ok) return;
    try {
      await api.delete(`/users/${u.id}`);
      toast("success", "User deleted", `"${u.fullName}" was removed.`);
      fetchData();
    } catch (e: any) {
      toast(
        "error",
        "Delete failed",
        e.response?.data?.message || "An error occurred.",
      );
    }
  };

  const handleSave = async () => {
    if (!form.fullName.trim()) {
      setFormError("Full name is required.");
      return;
    }
    if (!form.email.trim()) {
      setFormError("Email is required.");
      return;
    }
    if (!form.roleId) {
      setFormError("Please select a role.");
      return;
    }

    setSaving(true);
    setFormError("");
    try {
      if (editTarget) {
        await api.put(`/users/${editTarget.id}`, {
          fullName: form.fullName,
          email: form.email,
          roleId: form.roleId,
        });
        toast("success", "User updated", `"${form.fullName}" was saved.`);
      } else {
        if (!form.password.trim()) {
          setFormError("Password is required for new users.");
          setSaving(false);
          return;
        }
        await api.post("/users", {
          fullName: form.fullName,
          email: form.email,
          password: form.password,
          roleId: form.roleId,
        });
        toast(
          "success",
          "User created",
          `"${form.fullName}" was added successfully.`,
        );
      }
      setShowForm(false);
      fetchData();
    } catch (e: any) {
      setFormError(e.response?.data?.message || "Save failed.");
    }
    setSaving(false);
  };

  const getRoleColor = (name: string) => {
    if (name === "Admin") return "#a855f7";
    if (name === "Manager") return "#0ea5e9";
    return "#10b981";
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
      <div className="page-header">
        <div>
          <h1 style={{ marginBottom: "0.15rem" }}>Users</h1>
          <p style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>
            Manage user accounts and role assignments.
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
          <Button onClick={openCreate} icon={<Plus size={15} />}>
            New User
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

        {!loading && users.length === 0 && (
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
            <UsersIcon size={36} style={{ opacity: 0.25 }} />
            <p style={{ fontSize: "0.9rem" }}>No users found.</p>
          </div>
        )}

        {!loading && users.length > 0 && (
          <GlassPanel style={{ padding: "0", overflow: "hidden" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Full Name</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th style={{ width: 100, textAlign: "right" }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id}>
                    <td style={{ fontWeight: 500 }}>{u.fullName}</td>
                    <td style={{ color: "var(--text-secondary)" }}>
                      {u.email}
                    </td>
                    <td>
                      <span
                        style={{
                          display: "inline-flex",
                          alignItems: "center",
                          gap: "0.35rem",
                          padding: "0.15rem 0.6rem",
                          background: `${getRoleColor(u.roleName)}15`,
                          color: getRoleColor(u.roleName),
                          borderRadius: 99,
                          fontSize: "0.75rem",
                          fontWeight: 600,
                        }}
                      >
                        <span
                          style={{
                            width: 5,
                            height: 5,
                            borderRadius: "50%",
                            background: getRoleColor(u.roleName),
                          }}
                        />
                        {u.roleName}
                      </span>
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
                          onClick={() => openEdit(u)}
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
                            ((e.currentTarget as HTMLButtonElement).style.color =
                              "#fff")
                          }
                          onMouseLeave={(e) =>
                            ((e.currentTarget as HTMLButtonElement).style.color =
                              "var(--text-muted)")
                          }
                        >
                          <Pencil size={14} />
                        </button>
                        <button
                          onClick={() => handleDelete(u)}
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
                            ((e.currentTarget as HTMLButtonElement).style.color =
                              "#ef4444")
                          }
                          onMouseLeave={(e) =>
                            ((e.currentTarget as HTMLButtonElement).style.color =
                              "var(--text-muted)")
                          }
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </GlassPanel>
        )}
      </div>

      {showForm && (
        <Modal
          title={editTarget ? "Edit User" : "New User"}
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
            <FormField label="Full Name *">
              <InputField
                placeholder="e.g. John Doe"
                value={form.fullName}
                onChange={(e) =>
                  setForm({ ...form, fullName: e.target.value })
                }
              />
            </FormField>
            <FormField label="Email *">
              <InputField
                type="email"
                placeholder="e.g. john@example.com"
                value={form.email}
                onChange={(e) =>
                  setForm({ ...form, email: e.target.value })
                }
              />
            </FormField>
            {!editTarget && (
              <FormField label="Password *">
                <InputField
                  type="password"
                  placeholder="Enter password"
                  value={form.password}
                  onChange={(e) =>
                    setForm({ ...form, password: e.target.value })
                  }
                />
              </FormField>
            )}
            <FormField label="Role *">
              <select
                className="input-field"
                value={form.roleId}
                onChange={(e) =>
                  setForm({ ...form, roleId: e.target.value })
                }
              >
                <option value="">Select a role…</option>
                {roles.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
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
              <Button
                variant="secondary"
                onClick={() => setShowForm(false)}
              >
                Cancel
              </Button>
              <Button onClick={handleSave}>
                {saving
                  ? "Saving…"
                  : editTarget
                    ? "Update User"
                    : "Create User"}
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
