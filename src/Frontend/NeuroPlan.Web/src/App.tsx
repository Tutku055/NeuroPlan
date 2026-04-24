import React from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import { AuthProvider, useAuth } from "./context/AuthContext";
import { ToastProvider } from "./components/UI/Toast";
import { Login } from "./pages/Login";
import { Projects } from "./pages/Projects";
import { Tasks } from "./pages/Tasks";
import { Worklogs } from "./pages/Worklogs";
import { Performance } from "./pages/Performance";
import { Users } from "./pages/Users";
import { Roles } from "./pages/Roles";
import { DashboardLayout } from "./layouts/DashboardLayout";

const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
};

const DefaultRoute: React.FC = () => {
  return <Navigate to="/projects" replace />;
};

const LoginRoute: React.FC = () => {
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }
  return <Login />;
};

function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <Router>
          <Routes>
            <Route path="/login" element={<LoginRoute />} />

            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <DashboardLayout />
                </ProtectedRoute>
              }
            >
              <Route index element={<DefaultRoute />} />
              <Route path="projects" element={<Projects />} />
              <Route path="projects/:projectId/tasks" element={<Tasks />} />
              <Route path="worklogs" element={<Worklogs />} />
              <Route path="performance" element={<Performance />} />
              <Route path="users" element={<Users />} />
              <Route path="roles" element={<Roles />} />
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Router>
      </ToastProvider>
    </AuthProvider>
  );
}

export default App;
