import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { categoriesApi, technicianApi } from '../api/endpoints'
import { errorMessage } from '../api/client'
import { btnPrimary, btnSecondary, cardCls, errorCls, infoCls, inputCls, labelCls } from '../lib/ui'

export default function TechnicianProfile() {
  const qc = useQueryClient()
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: categoriesApi.list })
  const { data: me } = useQuery({ queryKey: ['technician', 'me'], queryFn: technicianApi.me })

  const [categoryId, setCategoryId] = useState('')
  const [lat, setLat] = useState('29.3084')
  const [lng, setLng] = useState('30.8428')
  const [available, setAvailable] = useState(true)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)

  // لو عنده بروفايل، نملى الفورم بيه
  useEffect(() => {
    if (me) {
      setCategoryId(String(me.serviceCategoryId))
      setLat(String(me.latitude))
      setLng(String(me.longitude))
      setAvailable(me.isAvailable)
    }
  }, [me])

  const save = useMutation({
    mutationFn: () =>
      technicianApi.upsert({
        serviceCategoryId: Number(categoryId),
        latitude: Number(lat),
        longitude: Number(lng),
        isAvailable: available,
      }),
    onSuccess: () => {
      setError('')
      setSaved(true)
      qc.invalidateQueries({ queryKey: ['technician'] })
    },
    onError: (e) => {
      setSaved(false)
      setError(errorMessage(e))
    },
  })

  const toggle = useMutation({
    mutationFn: () => technicianApi.setAvailability(!me?.isAvailable),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['technician'] }),
    onError: (e) => setError(errorMessage(e)),
  })

  function useMyLocation() {
    navigator.geolocation?.getCurrentPosition(
      (pos) => {
        setLat(pos.coords.latitude.toFixed(6))
        setLng(pos.coords.longitude.toFixed(6))
      },
      () => setError('مقدرناش نحدد موقعك. اكتب الإحداثيات يدوي.'),
    )
  }

  function submit(e: FormEvent) {
    e.preventDefault()
    setSaved(false)
    save.mutate()
  }

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <h1 className="text-xl font-bold">ملفي كفني</h1>

      {!me && <div className={infoCls}>املى بياناتك عشان تبدأ تستلم طلبات.</div>}

      {me && (
        <div className={`${cardCls} flex flex-wrap items-center justify-between gap-3`}>
          <div className="text-sm">
            <p>⭐ التقييم: <b>{me.averageRating}</b></p>
            <p>🔧 شغل مكتمل: <b>{me.completedJobsCount}</b></p>
          </div>
          <button className={me.isAvailable ? btnSecondary : btnPrimary} disabled={toggle.isPending} onClick={() => toggle.mutate()}>
            {me.isAvailable ? '🟢 متاح (اضغط لإيقاف الاستلام)' : '⚪ غير متاح (اضغط للتفعيل)'}
          </button>
        </div>
      )}

      <form onSubmit={submit} className={`${cardCls} space-y-4`}>
        {error && <div className={errorCls}>{error}</div>}
        {saved && <div className={infoCls}>تم الحفظ ✓</div>}

        <div>
          <label className={labelCls}>تخصصك</label>
          <select className={inputCls} required value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">اختار التخصص…</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </div>

        <div>
          <div className="mb-1 flex items-center justify-between">
            <label className={labelCls}>موقعك</label>
            <button type="button" onClick={useMyLocation} className="text-xs font-semibold text-blue-600">
              📍 استخدم موقعي
            </button>
          </div>
          <div className="grid grid-cols-2 gap-2" dir="ltr">
            <input className={inputCls} required type="number" step="any" value={lat} onChange={(e) => setLat(e.target.value)} />
            <input className={inputCls} required type="number" step="any" value={lng} onChange={(e) => setLng(e.target.value)} />
          </div>
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={available} onChange={(e) => setAvailable(e.target.checked)} />
          متاح لاستلام طلبات
        </label>

        <button className={btnPrimary} disabled={save.isPending}>
          {save.isPending ? 'جاري الحفظ…' : 'حفظ'}
        </button>
      </form>
    </div>
  )
}