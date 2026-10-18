import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/AuthContext'
import { notificationsApi } from '../api/endpoints'

const roleLabels = { Customer: 'عميل', Technician: 'فني', Admin: 'أدمن' } as const

function NotificationBell() {
  const { data: count = 0 } = useQuery({
    queryKey: ['notifications', 'count'],
    queryFn: notificationsApi.unreadCount,
    refetchInterval: 60_000,
  })

  return (
    <Link to="/notifications" className="relative rounded-lg p-2 text-xl hover:bg-slate-100" title="الإشعارات">
      🔔
      {count > 0 && (
        <span className="absolute -top-1 -end-1 min-w-5 rounded-full bg-red-600 px-1 text-center text-xs font-bold text-white">
          {count}
        </span>
      )}
    </Link>
  )
}

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const links =
    user?.role === 'Customer'
      ? [
          { to: '/requests', label: 'طلباتي' },
          { to: '/requests/new', label: 'طلب جديد' },
        ]
      : user?.role === 'Technician'
        ? [
            { to: '/requests', label: 'شغلي' },
            { to: '/technician/profile', label: 'ملفي' },
          ]
        : [{ to: '/admin', label: 'لوحة التحكم' }]

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-3 px-4 py-3">
          <Link to="/" className="text-lg font-bold text-blue-700">
            🛠️ فيكس فلو
          </Link>

          <nav className="flex flex-1 flex-wrap gap-1">
            {links.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                end
                className={({ isActive }) =>
                  `rounded-lg px-3 py-2 text-sm font-medium ${
                    isActive ? 'bg-blue-600 text-white' : 'text-slate-600 hover:bg-slate-100'
                  }`
                }
              >
                {l.label}
              </NavLink>
            ))}
          </nav>

          <NotificationBell />

          <div className="text-sm">
            <span className="font-semibold">{user?.fullName}</span>
            <span className="ms-1 text-xs text-slate-500">({user ? roleLabels[user.role] : ''})</span>
          </div>

          <button
            onClick={() => {
              logout()
              navigate('/login')
            }}
            className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm hover:bg-slate-50"
          >
            خروج
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}