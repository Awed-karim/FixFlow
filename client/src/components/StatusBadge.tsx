import { statusLabels, statusStyles } from '../lib/labels'
import type { RequestStatus } from '../api/types'

export default function StatusBadge({ status }: { status: RequestStatus }) {
  return (
    <span className={`inline-block rounded-full px-3 py-1 text-xs font-semibold ${statusStyles[status]}`}>
      {statusLabels[status]}
    </span>
  )
}