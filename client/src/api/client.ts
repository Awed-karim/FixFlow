import axios from 'axios'

export const STORAGE_KEY = 'fixflow_user'

// كل الطلبات بتروح لـ /api (والـ Vite proxy بيوصلها للـ API)
export const api = axios.create({ baseURL: '/api' })

// بيحط الـ Token في كل طلب تلقائياً
api.interceptors.request.use((config) => {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (raw) {
    try {
      const { token } = JSON.parse(raw) as { token: string }
      config.headers.Authorization = `Bearer ${token}`
    } catch {
      /* تجاهل */
    }
  }
  return config
})

// لو الـ Token انتهى (401) وإحنا مسجلين دخول، نرجّع لصفحة الدخول
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (axios.isAxiosError(err) && err.response?.status === 401 && localStorage.getItem(STORAGE_KEY)) {
      localStorage.removeItem(STORAGE_KEY)
      window.location.href = '/login'
    }
    return Promise.reject(err)
  },
)

// ترجمة أشهر رسائل الـ API
const known: Record<string, string> = {
  'Invalid email or password.': 'الإيميل أو كلمة المرور غير صحيحة',
  'No suitable technician is available right now.': 'مفيش فني مناسب متاح دلوقتي',
  'You must upload at least one Before photo and one After photo before completing.':
    'لازم ترفع صورة قبل وصورة بعد الإصلاح قبل ما تنهي الطلب',
  'This request is not assigned to you.': 'الطلب ده مش معيّن ليك',
  'You have not created your technician profile yet.': 'لسه ما عملتش ملفك كفني',
  'Role must be Customer or Technician.': 'الدور لازم يكون عميل أو فني',
}

// بيطلّع رسالة مفهومة من أي خطأ جاي من الـ API
export function errorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    if (!err.response) return 'تعذر الاتصال بالسيرفر. اتأكد إن الـ API شغال.'

    const d = err.response.data as
      | { error?: string; errors?: string[] | Record<string, string[]> }
      | undefined

    let msg: string | undefined
    const e = d?.errors
    if (d?.error) msg = d.error
    else if (Array.isArray(e)) msg = e.join(' - ')
    else if (e && typeof e === 'object') msg = Object.values(e).flat().join(' - ')

    if (msg) return known[msg] ?? msg
    if (err.response.status === 403) return 'مش مسموح لك بالعملية دي'
  }
  return 'حدث خطأ غير متوقع'
}