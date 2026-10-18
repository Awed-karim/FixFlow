import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/AuthContext'
import { requestsApi } from '../api/endpoints'
import { errorMessage } from '../api/client'
import StatusBadge from '../components/StatusBadge'
import StatusTimeline from '../components/StatusTimeline'
import PhotoDropzone from '../components/PhotoDropzone'
import { formatDate, photoLabels } from '../lib/labels'
import { btnDanger, btnPrimary, btnSecondary, btnSuccess, cardCls, errorCls, inputCls } from '../lib/ui'
import type { PhotoType } from '../api/types'

const CANCELLABLE = ['Pending', 'Assigned', 'Accepted']

export default function RequestDetails() {
  const { id } = useParams()
  const reqId = Number(id)
  const { user } = useAuth()
  const qc = useQueryClient()
  const [error, setError] = useState('')
  const [rating, setRating] = useState(5)
  const [comment, setComment] = useState('')

  const { data: r, isLoading, error: loadError } = useQuery({
    queryKey: ['requests', reqId],
    queryFn: () => requestsApi.get(reqId),
  })

  // رابط المشاركة (للعميل والأدمن بس)
  const canShare = user?.role === 'Customer' || user?.role === 'Admin'
  const share = useQuery({
    queryKey: ['requests', reqId, 'share'],
    queryFn: () => requestsApi.share(reqId),
    enabled: canShare && !!r,
  })

  const refresh = () => qc.invalidateQueries({ queryKey: ['requests'] })

  const act = useMutation({
    mutationFn: (v: { name: string; body?: unknown }) => requestsApi.action(reqId, v.name, v.body),
    onSuccess: () => {
      setError('')
      refresh()
    },
    onError: (e) => setError(errorMessage(e)),
  })

  const upload = useMutation({
    mutationFn: async (v: { type: PhotoType; files: File[] }) => {
      for (const f of v.files) await requestsApi.uploadPhoto(reqId, v.type, f)
    },
    onSuccess: () => {
      setError('')
      refresh()
    },
    onError: (e) => setError(errorMessage(e)),
  })

  const review = useMutation({
    mutationFn: () => requestsApi.review(reqId, { rating, comment }),
    onSuccess: () => {
      setError('')
      refresh()
    },
    onError: (e) => setError(errorMessage(e)),
  })

  if (isLoading) return <p className="text-slate-500">جاري التحميل…</p>
  if (loadError || !r) return <div className={errorCls}>{errorMessage(loadError)}</div>

  const role = user?.role
  const s = r.status
  const busy = act.isPending || upload.isPending || review.isPending

  const canCancel = (role === 'Customer' || role === 'Admin') && CANCELLABLE.includes(s)
  const canAddProblem = role === 'Customer' && CANCELLABLE.includes(s)
  const hasBefore = r.photos.some((p) => p.type === 'Before')
  const hasAfter = r.photos.some((p) => p.type === 'After')
  const showPhone = role === 'Customer' && r.technicianPhone && ['Accepted', 'OnTheWay', 'InProgress', 'Completed', 'Reviewed'].includes(s)

  function reject() {
    const reason = window.prompt('سبب الرفض (اختياري)')
    if (reason === null) return
    act.mutate({ name: 'reject', body: { reason } })
  }

  function cancel() {
    if (window.confirm('متأكد إنك عايز تلغي الطلب؟')) act.mutate({ name: 'cancel' })
  }

  function copyLink() {
    if (share.data) navigator.clipboard.writeText(share.data.trackingUrl)
    alert('تم نسخ رابط التتبع')
  }

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <Link to={role === 'Admin' ? '/admin' : '/requests'} className="text-sm text-blue-600">
        ← رجوع
      </Link>

      {error && <div className={errorCls}>{error}</div>}

      {/* بيانات الطلب */}
      <div className={`${cardCls} space-y-3`}>
        <div className="flex items-start justify-between gap-2">
          <h1 className="text-xl font-bold">{r.title}</h1>
          <StatusBadge status={s} />
        </div>
        <StatusTimeline status={s} />
        <p className="whitespace-pre-line text-slate-700">{r.description}</p>
        <div className="grid gap-1 text-sm text-slate-600 sm:grid-cols-2">
          <p>📍 {r.address}</p>
          <p>🏷️ {r.serviceCategoryName}</p>
          <p>🕒 اتنشأ: {formatDate(r.createdAt)}</p>
          {r.completedAt && <p>✅ اكتمل: {formatDate(r.completedAt)}</p>}
          {r.technicianName && <p>👷 الفني: {r.technicianName}</p>}
          {showPhone && (
            <p>
              📞{' '}
              <a href={`tel:${r.technicianPhone}`} className="font-semibold text-blue-600" dir="ltr">
                {r.technicianPhone}
              </a>
            </p>
          )}
        </div>
      </div>

      {/* الصور */}
      {(['Problem', 'Before', 'After'] as PhotoType[]).map((t) => {
        const photos = r.photos.filter((p) => p.type === t)
        if (photos.length === 0) return null
        return (
          <div key={t} className={cardCls}>
            <h2 className="mb-3 font-bold">{photoLabels[t]}</h2>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              {photos.map((p) => (
                <a key={p.url} href={p.url} target="_blank" rel="noreferrer">
                  <img src={p.url} alt={photoLabels[t]} className="h-36 w-full rounded-lg object-cover" />
                </a>
              ))}
            </div>
          </div>
        )
      })}

      {/* ===== إجراءات الفني ===== */}
      {role === 'Technician' && s === 'Assigned' && (
        <div className={`${cardCls} space-y-3`}>
          <p className="font-semibold text-sky-800">⏳ طلب جديد مرشح ليك. رد بسرعة قبل ما المهلة تخلص.</p>
          <div className="flex gap-2">
            <button className={btnSuccess} disabled={busy} onClick={() => act.mutate({ name: 'accept' })}>
              قبول الطلب
            </button>
            <button className={btnDanger} disabled={busy} onClick={reject}>
              رفض
            </button>
          </div>
        </div>
      )}

      {role === 'Technician' && s === 'Accepted' && (
        <div className={cardCls}>
          <button className={btnPrimary} disabled={busy} onClick={() => act.mutate({ name: 'on-the-way' })}>
            🚗 أنا في الطريق
          </button>
        </div>
      )}

      {role === 'Technician' && s === 'OnTheWay' && (
        <div className={cardCls}>
          <button className={btnPrimary} disabled={busy} onClick={() => act.mutate({ name: 'start' })}>
            🔧 وصلت وبدأت الشغل
          </button>
        </div>
      )}

      {role === 'Technician' && s === 'InProgress' && (
        <div className={`${cardCls} space-y-4`}>
          <h2 className="font-bold">صور الإصلاح (لازم صورة قبل وصورة بعد)</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            <PhotoDropzone label={`صور قبل الإصلاح ${hasBefore ? '✓' : ''}`} disabled={busy} onFiles={(files) => upload.mutate({ type: 'Before', files })} />
            <PhotoDropzone label={`صور بعد الإصلاح ${hasAfter ? '✓' : ''}`} disabled={busy} onFiles={(files) => upload.mutate({ type: 'After', files })} />
          </div>
          <button className={btnSuccess} disabled={busy || !hasBefore || !hasAfter} onClick={() => act.mutate({ name: 'complete' })}>
            ✅ تم الإصلاح
          </button>
          {(!hasBefore || !hasAfter) && <p className="text-xs text-slate-500">زرار "تم" هيشتغل أول ما ترفع الصورتين.</p>}
        </div>
      )}

      {/* ===== إجراءات العميل ===== */}
      {canAddProblem && (
        <div className={`${cardCls} space-y-2`}>
          <h2 className="font-bold">إضافة صور للمشكلة</h2>
          <PhotoDropzone label="اسحب صور المشكلة هنا" disabled={busy} onFiles={(files) => upload.mutate({ type: 'Problem', files })} />
        </div>
      )}

      {role === 'Customer' && s === 'Completed' && (
        <div className={`${cardCls} space-y-3`}>
          <h2 className="font-bold">راجع صور قبل/بعد وقيّم الخدمة</h2>
          <div className="flex gap-1 text-3xl" dir="ltr">
            {[1, 2, 3, 4, 5].map((n) => (
              <button key={n} type="button" onClick={() => setRating(n)} className={n <= rating ? 'text-amber-400' : 'text-slate-300'}>
                ★
              </button>
            ))}
          </div>
          <textarea className={inputCls} rows={3} placeholder="تعليقك (اختياري)" maxLength={1000} value={comment} onChange={(e) => setComment(e.target.value)} />
          <button className={btnPrimary} disabled={busy} onClick={() => review.mutate()}>
            إرسال التقييم
          </button>
        </div>
      )}

      {/* ===== إجراءات الأدمن ===== */}
      {role === 'Admin' && s === 'Pending' && (
        <div className={cardCls}>
          <button className={btnPrimary} disabled={busy} onClick={() => act.mutate({ name: 'assign' })}>
            ⚡ تعيين فني تلقائي دلوقتي
          </button>
        </div>
      )}

      {/* ===== مشاركة وإلغاء ===== */}
      {(canShare || canCancel) && (
        <div className={`${cardCls} flex flex-wrap gap-2`}>
          {canShare && share.data && (
            <>
              <a href={share.data.whatsAppUrl} target="_blank" rel="noreferrer" className={btnSuccess}>
                📲 شارك على واتساب
              </a>
              <button className={btnSecondary} onClick={copyLink}>
                🔗 انسخ رابط التتبع
              </button>
            </>
          )}
          {canCancel && (
            <button className={btnDanger} disabled={busy} onClick={cancel}>
              إلغاء الطلب
            </button>
          )}
        </div>
      )}
    </div>
  )
}