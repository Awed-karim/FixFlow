import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import ProtectedRoute from './auth/ProtectedRoute'
import Layout from './components/Layout'
import Login from './pages/Login'
import Register from './pages/Register'
import RequestsList from './pages/RequestsList'
import NewRequest from './pages/NewRequest'
import RequestDetails from './pages/RequestDetails'
import TechnicianProfile from './pages/TechnicianProfile'
import AdminBoard from './pages/AdminBoard'
import NotificationsPage from './pages/NotificationsPage'
import Track from './pages/Track'

// الصفحة الرئيسية: كل دور بيروح لصفحته
function Home() {
  const { user } = useAuth()
  if (!user) return <Navigate to="/login" replace />
  return <Navigate to={user.role === 'Admin' ? '/admin' : '/requests'} replace />
}

export default function App() {
  return (
    <Routes>
      {/* صفحات عامة */}
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/track/:token" element={<Track />} />

      {/* صفحات محمية (بتتفتح جوه الـ Layout) */}
      <Route
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route path="/requests" element={<RequestsList />} />
        <Route path="/requests/new" element={<ProtectedRoute roles={['Customer']}><NewRequest /></ProtectedRoute>} />
        <Route path="/requests/:id" element={<RequestDetails />} />
        <Route path="/technician/profile" element={<ProtectedRoute roles={['Technician']}><TechnicianProfile /></ProtectedRoute>} />
        <Route path="/admin" element={<ProtectedRoute roles={['Admin']}><AdminBoard /></ProtectedRoute>} />
        <Route path="/notifications" element={<NotificationsPage />} />
      </Route>

      <Route path="/" element={<Home />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}