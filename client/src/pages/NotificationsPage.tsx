import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { notificationsApi } from '../api/endpoints'
import { errorMessage } from '../api/client'
import { formatDate } from '../lib/labels'
import { btnSecondary, cardCls, errorCls } from '../lib/ui'

export default function NotificationsPage() {
  const qc = useQueryClient()
  const { data, isLoading, error } = useQuery({
    queryKey: ['notifications', 'list'],
    queryFn: () => notificationsApi.list({ page: 1, pageSize: 50 }),
  })

  const refresh = () => qc.invalidateQueries({ queryKey: ['notifications'] })
  const readOne = useMutation({ mutationFn: (id: number) => notificationsApi.read(id), onSuccess: refresh })
  const readAll = useMutation({ mutationFn: () => notificationsApi.readAll(), onSuccess: refresh })

  return (
    <div className="mx-auto max-w-2xl space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-bold">الإشعارات</h1>
        <button className={btnSecondary} disabled={readAll.isPending} onClick={() => readAll.mutate()}>
          تعليم الكل كمقروء
        </button>
      </div>

      {isLoading && <p className="text-slate-500">جاري التحميل…</p>}
      {error && <div className={errorCls}>{errorMessage(error)}</div>}
      {data && data.items.length === 0 && <div className={`${cardCls} text-center text-slate-500`}>مفيش إشعارات</div>}

      <div className="space-y-2">
        {data?.items.map((n) => (
          <div
            key={n.id}
            className={`${cardCls} flex items-start justify-between gap-3 ${n.isRead ? 'opacity-70' : 'border-blue-300 bg-blue-50'}`}
          >
            <div>
              <p className="text-sm">{n.message}</p>
              <p className="mt-1 text-xs text-slate-400">{formatDate(n.createdAt)}</p>
            </div>
            <div className="flex shrink-0 flex-col items-end gap-1 text-xs">
              {n.serviceRequestId && (
                <Link to={`/requests/${n.serviceRequestId}`} className="font-semibold text-blue-600" onClick={() => !n.isRead && readOne.mutate(n.id)}>
                  فتح الطلب
                </Link>
              )}
              {!n.isRead && (
                <button className="text-slate-500 underline" onClick={() => readOne.mutate(n.id)}>
                  مقروء
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}