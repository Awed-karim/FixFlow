import type { PhotoType, RequestStatus } from '../api/types'

export const statusLabels: Record<RequestStatus, string> = {
  Pending: 'جاري البحث عن فني',
  Assigned: 'في انتظار قبول الفني',
  Accepted: 'تم القبول',
  OnTheWay: 'الفني في الطريق',
  InProgress: 'جاري الإصلاح',
  Completed: 'تم الإصلاح',
  Reviewed: 'تم التقييم',
  Cancelled: 'ملغي',
  Rejected: 'مرفوض',
}

export const statusStyles: Record<RequestStatus, string> = {
  Pending: 'bg-amber-100 text-amber-800',
  Assigned: 'bg-sky-100 text-sky-800',
  Accepted: 'bg-blue-100 text-blue-800',
  OnTheWay: 'bg-indigo-100 text-indigo-800',
  InProgress: 'bg-violet-100 text-violet-800',
  Completed: 'bg-emerald-100 text-emerald-800',
  Reviewed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-red-100 text-red-800',
  Rejected: 'bg-red-100 text-red-800',
}

// خطوات الشريط الزمني (بالترتيب)
export const timelineSteps: { status: RequestStatus; label: string }[] = [
  { status: 'Pending', label: 'استلام' },
  { status: 'Assigned', label: 'ترشيح فني' },
  { status: 'Accepted', label: 'قبول' },
  { status: 'OnTheWay', label: 'في الطريق' },
  { status: 'InProgress', label: 'إصلاح' },
  { status: 'Completed', label: 'تم' },
  { status: 'Reviewed', label: 'تقييم' },
]

export const photoLabels: Record<PhotoType, string> = {
  Problem: 'صور المشكلة',
  Before: 'قبل الإصلاح',
  After: 'بعد الإصلاح',
}

// الـ API بيرجع التواريخ UTC (ساعات بتيجي من غير Z)، فبنضيفها
export function formatDate(s: string | null | undefined): string {
  if (!s) return '—'
  const hasTz = /(Z|[+-]\d{2}:\d{2})$/.test(s)
  return new Date(hasTz ? s : s + 'Z').toLocaleString('ar-EG', {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}