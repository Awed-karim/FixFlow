import { useState } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { categoriesApi, requestsApi } from '../api/endpoints'
import { errorMessage } from '../api/client'
import PhotoDropzone from '../components/PhotoDropzone'
import { btnPrimary, btnSecondary, cardCls, errorCls, inputCls, labelCls } from '../lib/ui'

export default function NewRequest() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: categoriesApi.list })

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [address, setAddress] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [lat, setLat] = useState('29.3084')
  const [lng, setLng] = useState('30.8428')
  const [files, setFiles] = useState<File[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  function useMyLocation() {
    if (!navigator.geolocation) {
      setError('المتصفح مش بيدعم تحديد الموقع')
      return
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLat(pos.coords.latitude.toFixed(6))
        setLng(pos.coords.longitude.toFixed(6))
      },
      () => setError('مقدرناش نحدد موقعك. اكتب الإحداثيات يدوي.'),
    )
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const created = await requestsApi.create({
        title,
        description,
        address,
        latitude: Number(lat),
        longitude: Number(lng),
        serviceCategoryId: Number(categoryId),
      })

      // رفع صور المشكلة (لو فشلت صورة، الطلب نفسه اتعمل)
      for (const f of files) {
        try {
          await requestsApi.uploadPhoto(created.id, 'Problem', f)
        } catch {
          /* تجاهل */
        }
      }

      qc.invalidateQueries({ queryKey: ['requests'] })
      navigate(`/requests/${created.id}`)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={submit} className={`${cardCls} mx-auto max-w-2xl space-y-4`}>
      <h1 className="text-xl font-bold">طلب صيانة جديد</h1>

      {error && <div className={errorCls}>{error}</div>}

      <div>
        <label className={labelCls}>نوع الخدمة</label>
        <select className={inputCls} required value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
          <option value="">اختار التخصص…</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label className={labelCls}>عنوان المشكلة</label>
        <input className={inputCls} required maxLength={200} placeholder="مثال: التكييف مش بيبرد" value={title} onChange={(e) => setTitle(e.target.value)} />
      </div>

      <div>
        <label className={labelCls}>وصف المشكلة</label>
        <textarea className={inputCls} required rows={4} maxLength={2000} value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>

      <div>
        <label className={labelCls}>العنوان</label>
        <input className={inputCls} required maxLength={500} value={address} onChange={(e) => setAddress(e.target.value)} />
      </div>

      <div>
        <div className="mb-1 flex items-center justify-between">
          <label className={labelCls}>الموقع (عشان نختار أقرب فني)</label>
          <button type="button" onClick={useMyLocation} className="text-xs font-semibold text-blue-600">
            📍 استخدم موقعي
          </button>
        </div>
        <div className="grid grid-cols-2 gap-2" dir="ltr">
          <input className={inputCls} required type="number" step="any" placeholder="Latitude" value={lat} onChange={(e) => setLat(e.target.value)} />
          <input className={inputCls} required type="number" step="any" placeholder="Longitude" value={lng} onChange={(e) => setLng(e.target.value)} />
        </div>
      </div>

      <div>
        <label className={labelCls}>صور المشكلة (اختياري)</label>
        <PhotoDropzone label="ارفع صور المشكلة" onFiles={(f) => setFiles((old) => [...old, ...f])} />
        {files.length > 0 && (
          <ul className="mt-2 space-y-1">
            {files.map((f, i) => (
              <li key={i} className="flex items-center justify-between rounded-lg bg-slate-50 px-3 py-1 text-sm">
                <span className="truncate" dir="ltr">{f.name}</span>
                <button type="button" className="text-red-600" onClick={() => setFiles((old) => old.filter((_, idx) => idx !== i))}>
                  ✕
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="flex gap-2">
        <button className={btnPrimary} disabled={loading}>
          {loading ? 'جاري الإرسال…' : 'إرسال الطلب'}
        </button>
        <button type="button" className={btnSecondary} onClick={() => navigate('/requests')}>
          إلغاء
        </button>
      </div>
    </form>
  )
}