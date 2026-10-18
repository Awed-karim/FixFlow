import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { errorMessage } from '../api/client'
import { btnPrimary, cardCls, errorCls, inputCls, labelCls } from '../lib/ui'

export default function Register() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<'Customer' | 'Technician'>('Customer')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const u = await register({ fullName, email, phoneNumber, password, role })
      navigate(u.role === 'Technician' ? '/technician/profile' : '/requests', { replace: true })
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md items-center px-4 py-8">
      <form onSubmit={submit} className={`${cardCls} w-full space-y-4`}>
        <h1 className="text-center text-2xl font-bold text-blue-700">إنشاء حساب</h1>

        {error && <div className={errorCls}>{error}</div>}

        <div className="grid grid-cols-2 gap-2">
          {(['Customer', 'Technician'] as const).map((r) => (
            <button
              type="button"
              key={r}
              onClick={() => setRole(r)}
              className={`rounded-lg border px-3 py-2 text-sm font-semibold ${
                role === r ? 'border-blue-600 bg-blue-50 text-blue-700' : 'border-slate-300 text-slate-600'
              }`}
            >
              {r === 'Customer' ? 'أنا عميل' : 'أنا فني'}
            </button>
          ))}
        </div>

        <div>
          <label className={labelCls}>الاسم بالكامل</label>
          <input className={inputCls} required value={fullName} onChange={(e) => setFullName(e.target.value)} />
        </div>
        <div>
          <label className={labelCls}>الإيميل</label>
          <input className={inputCls} type="email" required value={email} onChange={(e) => setEmail(e.target.value)} dir="ltr" />
        </div>
        <div>
          <label className={labelCls}>رقم الموبايل</label>
          <input className={inputCls} type="tel" required value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} dir="ltr" />
        </div>
        <div>
          <label className={labelCls}>كلمة المرور</label>
          <input className={inputCls} type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} dir="ltr" />
          <p className="mt-1 text-xs text-slate-500">8 حروف على الأقل، فيها حرف كبير وحرف صغير ورقم</p>
        </div>

        <button className={`${btnPrimary} w-full`} disabled={loading}>
          {loading ? 'جاري التسجيل…' : 'تسجيل'}
        </button>

        <p className="text-center text-sm text-slate-500">
          عندك حساب؟{' '}
          <Link to="/login" className="font-semibold text-blue-600">
            سجّل دخولك
          </Link>
        </p>
      </form>
    </div>
  )
}