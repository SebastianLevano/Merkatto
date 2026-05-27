import { PagedResult } from '../products/product.models';

export type { PagedResult };

export interface DailyClosingListItem {
  id: number;
  businessDate: string;
  grossIncome: number;
  netFlow: number;
  estimatedProfit: number;
}

export interface DailyClosingDetail {
  id: number;
  businessDate: string;
  cashAmount: number;
  yapeAmount: number;
  plinAmount: number;
  posAmount: number;
  posCommissionRate: number;
  posCommissionAmount: number;
  totalExpenses: number;
  quickPurchases: number;
  grossIncome: number;
  netFlow: number;
  estimatedProfit: number;
  notes: string | null;
  closedAt: string;
}

export interface DailyClosingPreview {
  businessDate: string;
  totalExpenses: number;
  alreadyClosed: boolean;
}

export interface CreateDailyClosingRequest {
  businessDate: string;
  cashAmount: number;
  yapeAmount: number;
  plinAmount: number;
  posAmount: number;
  posCommissionRate: number;
  quickPurchases: number;
  notes: string | null;
}
