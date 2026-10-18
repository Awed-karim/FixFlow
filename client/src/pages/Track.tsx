import { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { requestsApi } from '../api/endpoints'
import StatusBadge from '../components/StatusBadge'
import StatusTimeline from '../components/StatusTimeline'
import { formatDate, photoLabels } from '../lib/labels'
import { cardCls } from '../lib/ui'
import type { PhotoType } from '../api/types'

export default function Track() {
  const { token } = useParams()
  const qc = useQueryClient()

  const { data, isLoading, isError } = useQuery({
    queryKey: ['track', token],
    queryFn: () => requestsApi.track(token!),
    enabled: !!token,
  })

  // اتصال لحظي من غير Token: بنشترك في مجموعة الطلب ده بس
  useEffect(() => {
    if (!token) return

    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/updates')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build()

    const join = () => conn.invoke('JoinTracking', token).catch(() => {})

    conn.on('RequestUpdated', () => qc.invalidateQueries({ queryKey: ['track', token] }))
    conn.onreconnected(join)
    conn.start().then(join).catch(() => {})

    return () => {
      conn.stop()
    }
  }, [token, qc])

  return (
    <div className="mx-auto max-w-2xl space-y-4 px-4 py-8">
      <h1 className="text-center text-2xl font-bold text-blue-700">🛠️ فيكس فلو · تتبع الطلب</h1>

      {isLoading && <p className="text-center text-slate-500">جاري التحميل…</p>}
      {isError && <div className={`${cardCls} text-center text-red-600`}>رابط التتبع غير صحيح</div>}

      {data && (
        <>
          <div className={`${cardCls} space-y-3`}>
            <div className="flex items-start justify-between gap-2">
              <h2 className="text-lg font-bold">{data.title}</h2>
              <StatusBadge status={data.status} />
            </div>
            <StatusTimeline status={data.status} />
            <div className="grid gap-1 text-sm text-slate-600 sm:grid-cols-2">
              <p>🏷️ {data.serviceCategoryName}</p>
              <p>🕒 {formatDate(data.createdAt)}</p>
              {data.technicianName && <p>👷 الفني: {data.technicianName}</p>}
              {data.completedAt && <p>✅ اكتمل: {formatDate(data.completedAt)}</p>}
            </div>
            <p className="text-xs text-emerald-600">● الصفحة بتتحدث لحظياً</p>
          </div>

          {(['Before', 'After'] as PhotoType[]).map((t) => {
            const photos = data.photos.filter((p) => p.type === t)
            if (photos.length === 0) return null
            return (
              <div key={t} className={cardCls}>
                <h3 className="mb-3 font-bold">{photoLabels[t]}</h3>
                <div className="grid grid-cols-2 gap-3">
                  {photos.map((p) => (
                    <a key={p.url} href={p.url} target="_blank" rel="noreferrer">
                      <img src={p.url} alt={photoLabels[t]} className="h-40 w-full rounded-lg object-cover" />
                    </a>
                  ))}
                </div>
              </div>
            )
          })}
        </>
      )}
    </div>
  )
}