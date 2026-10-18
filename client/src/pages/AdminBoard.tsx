import { useState } from 'react'
import { Link } from 'react-router-dom'
import { DndContext, DragOverlay, PointerSensor, useDraggable, useDroppable, useSensor, useSensors } from '@dnd-kit/core'
import type { DragEndEvent, DragStartEvent } from '@dnd-kit/core'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { requestsApi } from '../api/endpoints'
import { errorMessage } from '../api/client'
import StatusBadge from '../components/StatusBadge'
import { formatDate, statusLabels } from '../lib/labels'
import { errorCls, infoCls } from '../lib/ui'
import type { RequestStatus, ServiceRequest } from '../api/types'

const COLUMNS: RequestStatus[] = ['Pending', 'Assigned', 'Accepted', 'OnTheWay', 'InProgress', 'Completed', 'Reviewed', 'Cancelled']
// الحالات اللي الأدمن يقدر يسحب منها
const MOVABLE: RequestStatus[] = ['Pending', 'Assigned', 'Accepted']

function CardView({ r }: { r: ServiceRequest }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3 shadow-sm">
      <Link to={`/requests/${r.id}`} className="block text-sm font-bold text-slate-800 hover:text-blue-600">
        #{r.id} {r.title}
      </Link>
      <p className="mt-1 truncate text-xs text-slate-500">📍 {r.address}</p>
      <p className="text-xs text-slate-500">{r.serviceCategoryName}</p>
      {r.technicianName && <p className="text-xs text-slate-600">👷 {r.technicianName}</p>}
      <div className="mt-2 flex items-center justify-between">
        <StatusBadge status={r.status} />
        <span className="text-[10px] text-slate-400">{formatDate(r.createdAt)}</span>
      </div>
    </div>
  )
}

function DraggableCard({ r }: { r: ServiceRequest }) {
  const enabled = MOVABLE.includes(r.status)
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: r.id, disabled: !enabled })

  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      style={{ opacity: isDragging ? 0.4 : 1, touchAction: enabled ? 'none' : undefined }}
      className={enabled ? 'cursor-grab' : ''}
    >
      <CardView r={r} />
    </div>
  )
}

function Column({ status, items }: { status: RequestStatus; items: ServiceRequest[] }) {
  const { setNodeRef, isOver } = useDroppable({ id: status })

  return (
    <div
      ref={setNodeRef}
      className={`w-64 shrink-0 rounded-xl p-2 ${isOver ? 'bg-blue-100' : 'bg-slate-100'}`}
    >
      <h3 className="mb-2 flex items-center justify-between px-1 text-sm font-bold text-slate-700">
        {statusLabels[status]}
        <span className="rounded-full bg-white px-2 text-xs">{items.length}</span>
      </h3>
      <div className="min-h-24 space-y-2">
        {items.map((r) => (
          <DraggableCard key={r.id} r={r} />
        ))}
      </div>
    </div>
  )
}

export default function AdminBoard() {
  const qc = useQueryClient()
  const [activeId, setActiveId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [info, setInfo] = useState('')

  // distance: عشان الضغط العادي (فتح الطلب) ما يتحسبش سحب
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 8 } }))

  const { data, isLoading, error: loadError } = useQuery({
    queryKey: ['requests', 'board'],
    queryFn: () => requestsApi.list({ page: 1, pageSize: 50 }),
  })

  const act = useMutation({
    mutationFn: (v: { id: number; name: string }) => requestsApi.action(v.id, v.name),
    onSuccess: () => {
      setError('')
      qc.invalidateQueries({ queryKey: ['requests'] })
    },
    onError: (e) => setError(errorMessage(e)),
  })

  const items = data?.items ?? []
  const activeReq = items.find((r) => r.id === activeId)

  function onDragStart(e: DragStartEvent) {
    setActiveId(Number(e.active.id))
    setError('')
    setInfo('')
  }

  function onDragEnd(e: DragEndEvent) {
    setActiveId(null)
    if (!e.over) return

    const r = items.find((x) => x.id === Number(e.active.id))
    const target = e.over.id as RequestStatus
    if (!r || r.status === target) return

    if (r.status === 'Pending' && target === 'Assigned') {
      act.mutate({ id: r.id, name: 'assign' })
    } else if (target === 'Cancelled' && MOVABLE.includes(r.status)) {
      if (window.confirm(`إلغاء الطلب #${r.id}؟`)) act.mutate({ id: r.id, name: 'cancel' })
    } else {
      setInfo('الانتقال ده بيتم من الفني أو العميل. من اللوحة تقدر بس تعيّن طلب معلق (اسحبه لـ "في انتظار قبول الفني") أو تلغي طلب.')
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold">لوحة التحكم</h1>
        <p className="text-sm text-slate-500">
          اسحب طلب <b>معلق</b> إلى "في انتظار قبول الفني" لتعيين فني تلقائي، أو اسحب أي طلب (قبل بدء الشغل) إلى "ملغي".
        </p>
      </div>

      {error && <div className={errorCls}>{error}</div>}
      {info && <div className={infoCls}>{info}</div>}
      {isLoading && <p className="text-slate-500">جاري التحميل…</p>}
      {loadError && <div className={errorCls}>{errorMessage(loadError)}</div>}

      <DndContext sensors={sensors} onDragStart={onDragStart} onDragEnd={onDragEnd} onDragCancel={() => setActiveId(null)}>
        <div className="flex gap-3 overflow-x-auto pb-4">
          {COLUMNS.map((status) => (
            <Column key={status} status={status} items={items.filter((r) => r.status === status)} />
          ))}
        </div>
        <DragOverlay>{activeReq ? <div className="w-60"><CardView r={activeReq} /></div> : null}</DragOverlay>
      </DndContext>

      {data && data.totalCount > 50 && (
        <p className="text-xs text-slate-500">بتظهر آخر 50 طلب من أصل {data.totalCount}.</p>
      )}
    </div>
  )
}