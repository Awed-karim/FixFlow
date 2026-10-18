import { timelineSteps } from '../lib/labels'
import type { RequestStatus } from '../api/types'

export default function StatusTimeline({ status }: { status: RequestStatus }) {
  if (status === 'Cancelled' || status === 'Rejected') {
    return <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">تم إلغاء الطلب</div>
  }

  const current = timelineSteps.findIndex((s) => s.status === status)

  return (
    <ol className="flex items-start justify-between gap-1 overflow-x-auto pb-1">
      {timelineSteps.map((step, i) => {
        const done = i < current
        const active = i === current
        return (
          <li key={step.status} className="flex min-w-14 flex-1 flex-col items-center gap-1 text-center">
            <span
              className={`flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold ${
                done
                  ? 'bg-emerald-600 text-white'
                  : active
                    ? 'bg-blue-600 text-white ring-4 ring-blue-100'
                    : 'bg-slate-200 text-slate-500'
              }`}
            >
              {done ? '✓' : i + 1}
            </span>
            <span className={`text-xs ${active ? 'font-bold text-blue-700' : 'text-slate-500'}`}>
              {step.label}
            </span>
          </li>
        )
      })}
    </ol>
  )
}