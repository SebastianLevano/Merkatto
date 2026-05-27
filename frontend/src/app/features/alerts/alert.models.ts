export enum AlertType { StockLow = 0, StockOut = 1, NoClosure = 2, HighPendingCredit = 3 }
export enum AlertSeverity { Info = 0, Warning = 1, Critical = 2 }

export interface AlertItem {
  type: AlertType;
  severity: AlertSeverity;
  message: string;
  reference: string | null;
}
