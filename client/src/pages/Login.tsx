import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { errorMessage } from '../api/client'
import { btnPrimary, cardCls, errorCls, inputCls, labelCls } from '../lib/ui'

export default function Login() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const u = await login(email, password)
      navigate(u.role === 'Admin' ? '/admin' : '/requests', { replace: true })
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="mx-auto flex min-h-screen max-w-md items-center px-4">
      <form onSubmit={submit} className={`${cardCls} w-full space-y-4`}>
        <h1 className="text-center text-2xl font-bold text-blue-700">🛠️ فيكس فلو</h1>
        <p className="text-center text-sm text-slate-500">سجّل دخولك للمتابعة</p>

        {error && <div className={errorCls}>{error}</div>}

        <div>
          <label className={labelCls}>الإيميل</label>
          <input className={inputCls} type="email" required value={email} onChange={(e) => setEmail(e.target.value)} dir="ltr" />
        </div>
        <div>
          <label className={labelCls}>كلمة المرور</label>
          <input className={inputCls} type="password" required value={password} onChange={(e) => setPassword(e.target.value)} dir="ltr" />
        </div>

        <button className={`${btnPrimary} w-full`} disabled={loading}>
          {loading ? 'جاري الدخول…' : 'دخول'}
        </button>

        <p className="text-center text-sm text-slate-500">
          معندكش حساب؟{' '}
          <Link to="/register" className="font-semibold text-blue-600">
            سجّل دلوقتي
          </Link>
        </p>
      </form>
    </div>
  )
}