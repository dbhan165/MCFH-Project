import axiosClient from './axiosClient';
import type { ScrapingJob, SystemProxy, UpsertSystemProxy } from '../types/admin';

export const adminApi = {
  listProxies: () =>
    axiosClient.get<SystemProxy[]>('/api/admin/proxies'),

  createProxy: (payload: UpsertSystemProxy) =>
    axiosClient.post<SystemProxy>('/api/admin/proxies', payload),

  updateProxy: (proxyId: number, payload: UpsertSystemProxy) =>
    axiosClient.put<SystemProxy>(`/api/admin/proxies/${proxyId}`, payload),

  deleteProxy: (proxyId: number) =>
    axiosClient.delete(`/api/admin/proxies/${proxyId}`),

  listScrapingJobs: () =>
    axiosClient.get<ScrapingJob[]>('/api/admin/scraping-jobs'),
};
