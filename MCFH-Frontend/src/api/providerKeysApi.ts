// Provider keys admin API client (Brevo + PayOS).
// Masked DTOs are returned by default; full key only via /reveal endpoint.

import axiosClient from './axiosClient';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';

const BASE = '/api/admin';

export interface BrevoKey {
  brevoKeyId: number;
  keyType: 'api' | 'smtp';
  smtpLogin?: string | null;
  fromAddress?: string | null;
  fromName?: string | null;
  status: 'active' | 'disabled' | string;
  isDefault: boolean;
  apiKeyMasked: string;
  note?: string | null;
  lastUsedAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  updatedBy?: number | null;
}

export interface PayOsKey {
  payOsKeyId: number;
  clientId: string;
  apiKeyMasked: string;
  checksumKeyMasked: string;
  environment: 'sandbox' | 'live' | string;
  status: 'active' | 'disabled' | string;
  isDefault: boolean;
  note?: string | null;
  lastUsedAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  updatedBy?: number | null;
}

const mapBrevo = (s: Record<string, unknown>): BrevoKey => ({
  brevoKeyId: pickNumber(s, 'brevoKeyId', 'BrevoKeyId'),
  keyType: (pickString(s, 'keyType', 'KeyType') || 'api') as BrevoKey['keyType'],
  smtpLogin: pickNullableString(s, 'smtpLogin', 'SmtpLogin'),
  fromAddress: pickNullableString(s, 'fromAddress', 'FromAddress'),
  fromName: pickNullableString(s, 'fromName', 'FromName'),
  status: pickString(s, 'status', 'Status'),
  isDefault: pickField(s, 'isDefault', 'IsDefault') === true,
  apiKeyMasked: pickString(s, 'apiKeyMasked', 'ApiKeyMasked'),
  note: pickNullableString(s, 'note', 'Note'),
  lastUsedAt: pickNullableString(s, 'lastUsedAt', 'LastUsedAt'),
  createdAt: pickString(s, 'createdAt', 'CreatedAt'),
  updatedAt: pickNullableString(s, 'updatedAt', 'UpdatedAt'),
  updatedBy: pickNumber(s, 'updatedBy', 'UpdatedBy'),
});

const mapPayOs = (s: Record<string, unknown>): PayOsKey => ({
  payOsKeyId: pickNumber(s, 'payOsKeyId', 'PayOsKeyId'),
  clientId: pickString(s, 'clientId', 'ClientId'),
  apiKeyMasked: pickString(s, 'apiKeyMasked', 'ApiKeyMasked'),
  checksumKeyMasked: pickString(s, 'checksumKeyMasked', 'ChecksumKeyMasked'),
  environment: pickString(s, 'environment', 'Environment') || 'live',
  status: pickString(s, 'status', 'Status'),
  isDefault: pickField(s, 'isDefault', 'IsDefault') === true,
  note: pickNullableString(s, 'note', 'Note'),
  lastUsedAt: pickNullableString(s, 'lastUsedAt', 'LastUsedAt'),
  createdAt: pickString(s, 'createdAt', 'CreatedAt'),
  updatedAt: pickNullableString(s, 'updatedAt', 'UpdatedAt'),
  updatedBy: pickNumber(s, 'updatedBy', 'UpdatedBy'),
});

export const providerKeysApi = {
  // ===== BREVO =====
  listBrevo: async (): Promise<BrevoKey[]> => {
    const res = await axiosClient.get<unknown[]>(`${BASE}/brevo-keys`);
    return (res.data ?? []).map((it) => mapBrevo(it as Record<string, unknown>));
  },

  createBrevo: async (payload: {
    keyType: string;
    apiKey: string;
    smtpLogin?: string;
    fromAddress?: string;
    fromName?: string;
    isDefault: boolean;
    note?: string;
  }): Promise<BrevoKey> => {
    const res = await axiosClient.post(`${BASE}/brevo-keys`, payload);
    return mapBrevo(res.data as Record<string, unknown>);
  },

  updateBrevo: async (
    id: number,
    payload: {
      apiKey?: string;
      smtpLogin?: string;
      fromAddress?: string;
      fromName?: string;
      status?: string;
      isDefault?: boolean;
      note?: string;
    },
  ): Promise<BrevoKey> => {
    const res = await axiosClient.patch(`${BASE}/brevo-keys/${id}`, payload);
    return mapBrevo(res.data as Record<string, unknown>);
  },

  deleteBrevo: async (id: number): Promise<void> => {
    await axiosClient.delete(`${BASE}/brevo-keys/${id}`);
  },

  revealBrevo: async (id: number): Promise<{ apiKey: string; smtpLogin?: string | null }> => {
    const res = await axiosClient.get(`${BASE}/brevo-keys/${id}/reveal`);
    return {
      apiKey: pickString(res.data as Record<string, unknown>, 'apiKey', 'ApiKey'),
      smtpLogin: pickNullableString(res.data as Record<string, unknown>, 'smtpLogin', 'SmtpLogin'),
    };
  },

  // ===== PAYOS =====
  listPayOs: async (): Promise<PayOsKey[]> => {
    const res = await axiosClient.get<unknown[]>(`${BASE}/payos-keys`);
    return (res.data ?? []).map((it) => mapPayOs(it as Record<string, unknown>));
  },

  createPayOs: async (payload: {
    clientId: string;
    apiKey: string;
    checksumKey: string;
    environment: string;
    isDefault: boolean;
    note?: string;
  }): Promise<PayOsKey> => {
    const res = await axiosClient.post(`${BASE}/payos-keys`, payload);
    return mapPayOs(res.data as Record<string, unknown>);
  },

  updatePayOs: async (
    id: number,
    payload: {
      clientId?: string;
      apiKey?: string;
      checksumKey?: string;
      environment?: string;
      status?: string;
      isDefault?: boolean;
      note?: string;
    },
  ): Promise<PayOsKey> => {
    const res = await axiosClient.patch(`${BASE}/payos-keys/${id}`, payload);
    return mapPayOs(res.data as Record<string, unknown>);
  },

  deletePayOs: async (id: number): Promise<void> => {
    await axiosClient.delete(`${BASE}/payos-keys/${id}`);
  },

  revealPayOs: async (id: number): Promise<{ clientId: string; apiKey: string; checksumKey: string }> => {
    const res = await axiosClient.get(`${BASE}/payos-keys/${id}/reveal`);
    return {
      clientId: pickString(res.data as Record<string, unknown>, 'clientId', 'ClientId'),
      apiKey: pickString(res.data as Record<string, unknown>, 'apiKey', 'ApiKey'),
      checksumKey: pickString(res.data as Record<string, unknown>, 'checksumKey', 'ChecksumKey'),
    };
  },
};
