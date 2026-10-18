import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/AuthContext'
import { requestsApi, technicianApi } from '../api/endpoints'
import { errorMessage } from '../api/client'
import StatusBadge from '../components/StatusBadge'
import { formatDate, statusLabels } from '../lib/labels'
import { btnPrimary, btnSecondary, cardCls, errorCls, infoCls, inputCls } from '../lib/ui'
import type { RequestStatus } from '../api/types'

export default function RequestsList() {
  const { user } = useAuth()
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)

  const { data, isLoading, error } = useQuery({
    queryKey: ['requests', 'list', status, page],
    queryFn: () => requestsApi.list({ status: status || undefined, page, pageSize: 10 }),
  })

  // الفني اللي لسه ما عملش بروفايل
  const tech = useQuery({
    queryKey: ['technician', 'me'],
    queryFn: technicianApi.me,
    enabled: user?.role === 'Technician',
  })
  const noProfile = user?.role === 'Technician' && tech.isError

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-bold">{user?.role === 'Technician' ? 'الشغل المعيّن ليا' : 'طلباتي'}</h1>

        <div className="flex gap-2">
          <select
            className={`${inputCls} w-auto`}
            value={status}
            onChange={(e) => {
              setStatus(e.target.value)
              setPage(1)
            }}
          >
            <option value="">كل الحالات</option>
            {(Object.keys(statusLabels) as RequestStatus[]).map((s) => (
              <option key={s} value={s}>
                {statusLabels[s]}
              </option>
            ))}
          </select>
          {user?.role === 'Customer' && (
            <Link to="/requests/new" className={btnPrimary}>
              + طلب جديد
            </Link>
          )}
        </div>
      </div>

      {noProfile && (
        <div className={infoCls}>
          لسه ما عملتش ملفك كفني. مش هتستلم طلبات قبل ما تعمله.{' '}
          <Link to="/technician/profile" className="font-bold underline">
            اعمله دلوقتي
          </Link>
        </div>
      )}

      {isLoading && <p className="text-slate-500">جاري التحميل…</p>}
      {error && <div className={errorCls}>{errorMessage(error)}</div>}
      {data && data.items.length === 0 && <div className={`${cardCls} text-center text-slate-500`}>مفيش طلبات</div>}

      <div className="grid gap-3 md:grid-cols-2">
        {data?.items.map((r) => (
          <Link key={r.id} to={`/requests/${r.id}`} className={`${cardCls} block transition hover:border-blue-400`}>
            <div className="flex items-start justify-between gap-2">
              <h3 className="font-bold">{r.title}</h3>
              <StatusBadge status={r.status} />
            </div>
            <p className="mt-1 text-sm text-slate-600">📍 {r.address}</p>
            <p className="mt-1 text-xs text-slate-500">
              {r.serviceCategoryName}
              {r.technicianName ? ` · الفني: ${r.technicianName}` : ''}
            </p>
            <p className="mt-2 text-xs text-slate-400">{formatDate(r.createdAt)}</p>
          </Link>
        ))}
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-3">
          <button className={btnSecondary} disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            السابق
          </button>
          <span className="text-sm text-slate-600">
            صفحة {data.page} من {data.totalPages}
          </span>
          <button className={btnSecondary} disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>
            التالي
          </button>
        </div>
      )}
    </div>
  )
}