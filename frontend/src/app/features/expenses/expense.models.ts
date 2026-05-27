import { PagedResult } from '../products/product.models';

export type { PagedResult };

export enum ExpenseType {
  Luz = 1,
  Agua = 2,
  Movilidad = 3,
  Reposicion = 4,
  Mantenimiento = 5,
  CompraRapida = 6,
  Otros = 99
}

export const EXPENSE_LABEL: Record<ExpenseType, string> = {
  [ExpenseType.Luz]: 'Luz',
  [ExpenseType.Agua]: 'Agua',
  [ExpenseType.Movilidad]: 'Movilidad',
  [ExpenseType.Reposicion]: 'Reposición',
  [ExpenseType.Mantenimiento]: 'Mantenimiento',
  [ExpenseType.CompraRapida]: 'Compra rápida',
  [ExpenseType.Otros]: 'Otros'
};

export interface ExpenseItem {
  id: number;
  date: string;
  type: ExpenseType;
  amount: number;
  description: string | null;
}

export interface CreateExpenseRequest {
  date: string;
  type: ExpenseType;
  amount: number;
  description: string | null;
}

export interface ExpenseByType {
  type: ExpenseType;
  total: number;
}

export interface ExpenseSummary {
  total: number;
  byType: ExpenseByType[];
}
