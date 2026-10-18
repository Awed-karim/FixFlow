import { api } from './client'
import type {
  AppNotification, AuthUser, Category, CreateRequestBody, Paged, Photo, PhotoType,
  RegisterBody, ServiceRequest, ShareLinks, Technician, Tracking, UpsertTechnicianBody,
} from './types'

export const authApi = {
  login: (b: { email: string; password: string }) =>
    api.post<AuthUser>('/auth/login', b).then((r) => r.data),
  register: (b: RegisterBody) =>
    api.post<AuthUser>('/auth/register', b).then((r) => r.data),
}

export const categoriesApi = {
  list: () => api.get<Category[]>('/categories').then((r) => r.data),
}

export const requestsApi = {
  list: (params: { status?: string; page?: number; pageSize?: number }) =>
    api.get<Paged<ServiceRequest>>('/requests', { params }).then((r) => r.data),

  get: (id: number) =>
    api.get<ServiceRequest>(`/requests/${id}`).then((r) => r.data),

  create: (b: CreateRequestBody) =>
    api.post<ServiceRequest>('/requests', b).then((r) => r.data),

  // accept | reject | on-the-way | start | complete | assign | cancel
  action: (id: number, name: string, body?: unknown) =>
    api.post<ServiceRequest>(`/requests/${id}/${name}`, body).then((r) => r.data),

  review: (id: number, b: { rating: number; comment: string }) =>
    api.post<ServiceRequest>(`/requests/${id}/review`, b).then((r) => r.data),

  uploadPhoto: (id: number, type: PhotoType, file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    return api.post<Photo>(`/requests/${id}/photos`, fd, { params: { type } }).then((r) => r.data)
  },

  share: (id: number) =>
    api.get<ShareLinks>(`/requests/${id}/share`).then((r) => r.data),

  track: (token: string) =>
    api.get<Tracking>(`/requests/track/${token}`).then((r) => r.data),
}

export const technicianApi = {
  me: () => api.get<Technician>('/technicians/me').then((r) => r.data),
  upsert: (b: UpsertTechnicianBody) =>
    api.put<Technician>('/technicians/me', b).then((r) => r.data),
  setAvailability: (isAvailable: boolean) =>
    api.put<Technician>('/technicians/me/availability', { isAvailable }).then((r) => r.data),
}

export const notificationsApi = {
  list: (params: { page?: number; pageSize?: number }) =>
    api.get<Paged<AppNotification>>('/notifications', { params }).then((r) => r.data),
  unreadCount: () =>
    api.get<{ count: number }>('/notifications/unread-count').then((r) => r.data.count),
  read: (id: number) => api.post(`/notifications/${id}/read`),
  readAll: () => api.post('/notifications/read-all'),
}