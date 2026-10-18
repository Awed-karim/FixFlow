import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/AuthContext'
import type { AppNotification } from '../api/types'

interface Toast {
  id: number
  text: string
}

export function RealtimeProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const qc = useQueryClient()
  const [toasts, setToasts] = useState<Toast[]>([])
  const token = user?.token

  useEffect(() => {
    if (!token) return

    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/updates', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.None)
      .build()

    // أي تحديث في طلب → نعيد تحميل بيانات الطلبات
    conn.on('RequestUpdated', () => {
      qc.invalidateQueries({ queryKey: ['requests'] })
    })

    // إشعار جديد → نحدّث الجرس ونطلّع رسالة صغيرة
    conn.on('NotificationReceived', (n: AppNotification) => {
      qc.invalidateQueries({ queryKey: ['notifications'] })
      const id = Date.now() + Math.random()
      setToasts((t) => [...t, { id, text: n.message }])
      setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 6000)
    })

    conn.start().catch(() => {})

    return () => {
      conn.stop()
    }
  }, [token, qc])

  return (
    <>
      {children}
      <div className="fixed bottom-4 start-4 z-50 flex w-80 max-w-[90vw] flex-col gap-2">
        {toasts.map((t) => (
          <div key={t.id} className="rounded-xl bg-slate-900 px-4 py-3 text-sm text-white shadow-lg">
            🔔 {t.text}
          </div>
        ))}
      </div>
    </>
  )
}