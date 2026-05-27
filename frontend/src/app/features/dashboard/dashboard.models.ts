export interface DashboardLastClosing {
  date: string;
  grossIncome: number;
  netFlow: number;
  estimatedProfit: number;
}

export interface DashboardClosingRow {
  date: string;
  grossIncome: number;
  netFlow: number;
}

export interface DashboardSummary {
  lastClosing: DashboardLastClosing | null;
  todayExpenses: number;
  lowStockCount: number;
  totalCreditBalance: number;
  activeCreditCustomers: number;
  recentClosings: DashboardClosingRow[];
}
