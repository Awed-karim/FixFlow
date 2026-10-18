export type Role = 'Customer' | 'Technician' | 'Admin'

export type RequestStatus =
  | 'Pending' | 'Assigned' | 'Accepted' | 'OnTheWay' | 'InProgress'
  | 'Completed' | 'Reviewed' | 'Cancelled' | 'Rejected'

export type PhotoType = 'Problem' | 'Before' | 'After'

export interface AuthUser {
  userId: string
  fullName: string
  email: string
  role: Role
  token: string
  expiresAt: string
}

export interface RegisterBody {
  fullName: string
  email: string
  phoneNumber: string
  password: string
  role: 'Customer' | 'Technician'
}

export interface Category {
  id: number
  name: string
  description?: string | null
}

export interface Photo {
  url: string
  type: PhotoType
}

export interface ServiceRequest {
  id: number
  title: string
  description: string
  address: string
  latitude: number
  longitude: number
  status: RequestStatus
  serviceCategoryId: number
  serviceCategoryName: string
  technicianProfileId: number | null
  technicianName: string | null
  technicianPhone: string | null
  trackingToken: string
  createdAt: string
  assignedAt: string | null
  completedAt: string | null
  photos: Photo[]
}

export interface CreateRequestBody {
  title: string
  description: string
  address: string
  latitude: number
  longitude: number
  serviceCategoryId: number
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface Technician {
  id: number
  userId: string
  fullName: string
  phoneNumber: string
  serviceCategoryId: number
  serviceCategoryName: string
  latitude: number
  longitude: number
  isAvailable: boolean
  averageRating: number
  completedJobsCount: number
}

export interface UpsertTechnicianBody {
  serviceCategoryId: number
  latitude: number
  longitude: number
  isAvailable: boolean
}

export interface Tracking {
  title: string
  status: RequestStatus
  serviceCategoryName: string
  technicianName: string | null
  createdAt: string
  assignedAt: string | null
  completedAt: string | null
  photos: Photo[]
}

export interface ShareLinks {
  trackingUrl: string
  whatsAppUrl: string
  message: string
}

export interface AppNotification {
  id: number
  message: string
  type: string
  serviceRequestId: number | null
  isRead: boolean
  createdAt: string
}