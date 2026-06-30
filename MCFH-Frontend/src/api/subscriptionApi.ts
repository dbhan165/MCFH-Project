import axiosClient from './axiosClient';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';

export interface SubscriptionPlan {
  planId: number;
  name: string;
  description: string;
  price: number;
  priceLabel: string;
  aiCreditLimit: number;
  features: string[];
  buttonText: string;
  isPopular: boolean;
  activeSubscribers: number;
}

export interface BillingSummary {
  workspaceId: number | null;
  workspaceName: string;
  planId: number | null;
  planName: string;
  status: string;
  expiryDate: string | null;
  renewalNote: string | null;
  projectUsed: number;
  projectLimit: number;
  mentionUsed: number;
  mentionLimit: number;
  memberUsed: number;
  memberLimit: number;
  aiCreditUsed: number;
  aiCreditLimit: number;
}

export interface PaymentHistory {
  paymentId: number;
  transactionRef: string | null;
  amount: number;
  amountLabel: string;
  status: string | null;
  type: string | null;
  planName: string | null;
  createdAt: string | null;
}

function mapPlan(data: Record<string, unknown>): SubscriptionPlan {
  return {
    planId: pickNumber(data, 'planId', 'PlanId'),
    name: pickString(data, 'name', 'Name'),
    description: pickString(data, 'description', 'Description'),
    price: Number(pickField(data, 'price', 'Price') ?? 0),
    priceLabel: pickString(data, 'priceLabel', 'PriceLabel'),
    aiCreditLimit: pickNumber(data, 'aiCreditLimit', 'AiCreditLimit'),
    features: (pickField<string[]>(data, 'features', 'Features') ?? []).filter(Boolean),
    buttonText: pickString(data, 'buttonText', 'ButtonText'),
    isPopular: pickField(data, 'isPopular', 'IsPopular') === true,
    activeSubscribers: pickNumber(data, 'activeSubscribers', 'ActiveSubscribers'),
  };
}

function mapBilling(data: Record<string, unknown>): BillingSummary {
  return {
    workspaceId: pickField<number>(data, 'workspaceId', 'WorkspaceId') ?? null,
    workspaceName: pickString(data, 'workspaceName', 'WorkspaceName'),
    planId: pickField<number>(data, 'planId', 'PlanId') ?? null,
    planName: pickString(data, 'planName', 'PlanName'),
    status: pickString(data, 'status', 'Status'),
    expiryDate: pickNullableString(data, 'expiryDate', 'ExpiryDate'),
    renewalNote: pickNullableString(data, 'renewalNote', 'RenewalNote'),
    projectUsed: pickNumber(data, 'projectUsed', 'ProjectUsed'),
    projectLimit: pickNumber(data, 'projectLimit', 'ProjectLimit'),
    mentionUsed: pickNumber(data, 'mentionUsed', 'MentionUsed'),
    mentionLimit: pickNumber(data, 'mentionLimit', 'MentionLimit'),
    memberUsed: pickNumber(data, 'memberUsed', 'MemberUsed'),
    memberLimit: pickNumber(data, 'memberLimit', 'MemberLimit'),
    aiCreditUsed: pickNumber(data, 'aiCreditUsed', 'AiCreditUsed'),
    aiCreditLimit: pickNumber(data, 'aiCreditLimit', 'AiCreditLimit'),
  };
}

function mapPayment(data: Record<string, unknown>): PaymentHistory {
  return {
    paymentId: pickNumber(data, 'paymentId', 'PaymentId'),
    transactionRef: pickNullableString(data, 'transactionRef', 'TransactionRef'),
    amount: Number(pickField(data, 'amount', 'Amount') ?? 0),
    amountLabel: pickString(data, 'amountLabel', 'AmountLabel'),
    status: pickNullableString(data, 'status', 'Status'),
    type: pickNullableString(data, 'type', 'Type'),
    planName: pickNullableString(data, 'planName', 'PlanName'),
    createdAt: pickNullableString(data, 'createdAt', 'CreatedAt'),
  };
}

export const subscriptionApi = {
  getPlans: async (): Promise<SubscriptionPlan[]> => {
    const response = await axiosClient.get<unknown[]>('/api/subscription/plans');
    return (response.data ?? []).map((item) => mapPlan(item as Record<string, unknown>));
  },

  getBilling: async (workspaceId?: number): Promise<BillingSummary> => {
    const response = await axiosClient.get<Record<string, unknown>>('/api/subscription/billing', {
      params: workspaceId ? { workspaceId } : undefined,
    });
    return mapBilling(response.data);
  },

  getPayments: async (): Promise<PaymentHistory[]> => {
    const response = await axiosClient.get<unknown[]>('/api/subscription/payments');
    return (response.data ?? []).map((item) => mapPayment(item as Record<string, unknown>));
  },

  subscribe: async (workspaceId: number, planId: number): Promise<BillingSummary> => {
    const response = await axiosClient.post<Record<string, unknown>>('/api/subscription/subscribe', {
      workspaceId,
      planId,
    });
    return mapBilling(response.data);
  },
};
