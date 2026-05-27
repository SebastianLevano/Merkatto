export interface NrusMonthEstimate {
  year: number;
  month: number;
  monthlyIncome: number;
  monthlyPurchases: number;
  maxAmount: number;
  category: number | null;
  quota: number | null;
  exceedsLimit: boolean;
}
