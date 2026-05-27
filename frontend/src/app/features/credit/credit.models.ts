export interface CreditCustomerListItem {
  id: number;
  name: string;
  phone: string | null;
  balance: number;
}

export interface CreditHistoryEntry {
  kind: 'sale' | 'payment';
  id: number;
  date: string;
  amount: number;
  detail: string | null;
}

export interface CreditCustomerDetail {
  id: number;
  name: string;
  phone: string | null;
  notes: string | null;
  balance: number;
  history: CreditHistoryEntry[];
}

export interface SaveCustomerRequest {
  name: string;
  phone: string | null;
  notes: string | null;
}

export interface CreditSaleItemRequest {
  description: string;
  quantity: number;
  lineTotal: number;
}

export interface CreateCreditSaleRequest {
  customerId: number;
  date: string;
  notes: string | null;
  amount: number | null;
  items: CreditSaleItemRequest[] | null;
}

export interface CreatePaymentRequest {
  customerId: number;
  date: string;
  amount: number;
  notes: string | null;
}
