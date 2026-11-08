const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5154';

export interface ActiveCommitment {
  id: number;
  title: string;
  amount: number;
  badge: string; // e.g. "1/5" or "Fixo" or "Mensal"
  commitmentType?: 'PIX_COMMITMENT' | 'SUBSCRIPTION';
  paymentSource?: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT';
  billingDay?: number | null;
  lastPaidCycle?: string | null;
  isPaidInCycle?: boolean;
}

export interface CategoryBreakdown {
  category: string;
  amount: number;
  percentage: number;
}

export interface DashboardSummary {
  billingCycle: string;
  cycleClosingDate: string;
  cycleDueDate: string;
  cycleState: 'CURRENT' | 'FUTURE' | 'CLOSED';
  totalIncome: number;
  incomeFirstTranche: number;
  incomeSecondTranche: number;
  monthlyInvestmentGoal: number;
  creditCardLimit: number;
  customCardCap: number | null;
  availableBankCreditLimit: number;
  activeFixedCommitments: ActiveCommitment[];
  totalFixedCommitments: number;
  activeSubscriptions: ActiveCommitment[];
  cardSubscriptionsTotal: number;
  cardHardCap: number;
  cardHardCapFormula: number;
  currentInvoiceTotal: number;
  pixCaixinhaUsedInCycle: number;
  remainingBudget: number;
  daysUntilClosing: number;
  availablePerDay: number;
  status: 'HEALTHY' | 'WARNING' | 'INVADING_INVESTMENT';
  cdiAnnualRate: number;
  phantomYieldEstimate: number;
  phantomYieldFullCap: number;

  // Nubank Caixinha (115% CDI) Tracker
  nubankCaixinhaBalance: number;
  nubankCaixinhaCdiRate: number;
  nubankCaixinhaEffectiveAnnualRate: number;
  nubankCaixinhaMonthlyYield: number;
  nubankCaixinhaDailyYield: number;

  // Pace Tracker
  daysElapsed: number;
  totalDaysInCycle: number;
  idealPaceAmount: number;
  paceStatus: 'AHEAD' | 'ON_TRACK' | 'OVERPACED';
  paceDifference: number;

  categoryBreakdown: CategoryBreakdown[];
}

export interface CardExpense {
  id: number;
  description: string;
  category: string;
  amount: number;
  transactionDate: string;
  billingCycle: string;
  installmentCurrent: number;
  installmentTotal: number;
  purchaseGroupId: string | null;
  paymentMethod: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT';
  deductFromCycleCap: boolean;
}

export interface ProjectionCycle {
  billingCycle: string;
  fixedCommitmentsTotal: number;
  committedCardInstallments: number;
  projectedFreeCap: number;
  projectedDailyBurnRate: number;
}

export interface SimulatorCycleImpact {
  billingCycle: string;
  projectedFreeCapBefore: number;
  projectedFreeCapAfter: number;
  installmentAmount: number;
  willInvade: boolean;
}

export interface SimulatorResponse {
  amount: number;
  installments: number;
  installmentValue: number;
  currentDailyBefore: number;
  currentDailyAfter: number;
  remainingBudgetBefore: number;
  remainingBudgetAfter: number;
  willInvadeInvestmentGoal: boolean;
  invadedAmount: number;
  affectedCycles: SimulatorCycleImpact[];
}

export interface ChatCommandResponse {
  reply: string;
  intent: string;
  dashboard: DashboardSummary;
}

export interface FinancialProfile {
  id: number;
  incomeFirstTranche: number;
  incomeSecondTranche: number;
  monthlyInvestmentGoal: number;
  closingDay: number;
  dueDay: number;
  cdiAnnualRate: number;
  nubankCaixinhaBalance: number;
  nubankCaixinhaCdiRate: number;
  creditCardLimit: number;
  customCardCap: number | null;
  totalIncome: number;
}

export interface FixedCommitment {
  id: number;
  title: string;
  amount: number;
  startCycle: string;
  totalInstallments: number | null;
  isActive: boolean;
  commitmentType?: 'PIX_COMMITMENT' | 'SUBSCRIPTION';
  paymentSource?: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT';
  billingDay?: number | null;
  lastPaidCycle?: string | null;
}

async function request<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  const res = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    cache: 'no-store',
  });

  if (!res.ok) {
    const errorText = await res.text();
    throw new Error(errorText || `API error: ${res.status}`);
  }

  return res.json();
}

export const api = {
  // System
  getSystemStatus: () => request<{ status: string; activeBillingCycle: string }>(`/api/system/status`),

  // Dashboard
  getDashboard: (cycle?: string) =>
    request<DashboardSummary>(`/api/dashboard${cycle ? `?cycle=${cycle}` : ''}`),

  getProjections: (months = 6) =>
    request<ProjectionCycle[]>(`/api/dashboard/projections?months=${months}`),

  // Expenses
  getExpenses: (cycle?: string) =>
    request<CardExpense[]>(`/api/expenses${cycle ? `?cycle=${cycle}` : ''}`),

  createExpense: (data: {
    description: string;
    category: string;
    totalAmount: number;
    installments: number;
    transactionDate?: string;
    paymentMethod?: 'CARD' | 'PIX_CAIXINHA' | 'PIX_ACCOUNT';
    deductFromCycleCap?: boolean;
  }) => request<CardExpense[]>(`/api/expenses`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),

  updateCardLimit: (creditCardLimit: number, customCardCap: number | null) =>
    request<{ message: string; creditCardLimit: number; customCardCap: number | null }>(`/api/profile/card-limit`, {
      method: 'PATCH',
      body: JSON.stringify({ creditCardLimit, customCardCap }),
    }),

  payCommitmentFromCaixinha: (id: number, cycle?: string) =>
    request<{ message: string; cycle: string }>(`/api/commitments/${id}/pay-from-caixinha${cycle ? `?cycle=${cycle}` : ''}`, {
      method: 'POST',
    }),

  createPixCaixinhaExpense: (data: {
    description: string;
    category: string;
    amount: number;
    deductFromCycleCap: boolean;
    transactionDate?: string;
  }) => request<CardExpense>(`/api/expenses/pix-caixinha`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),

  updateExpense: (id: number, data: {
    description: string;
    category: string;
    amount: number;
    transactionDate: string;
    updateAllGroupInstallments?: boolean;
  }) => request<{ message: string }>(`/api/expenses/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  }),

  deleteExpense: (id: number, deleteAllInstallments = false) =>
    request<{ message: string }>(`/api/expenses/${id}?deleteAllInstallments=${deleteAllInstallments}`, {
      method: 'DELETE',
    }),

  undoExpense: () =>
    request<{ message: string }>(`/api/expenses/undo`, {
      method: 'POST',
    }),

  syncInvoice: (targetTotal: number, cycle?: string) =>
    request<{ message: string; currentInvoiceTotal: number }>(`/api/expenses/sync-invoice`, {
      method: 'POST',
      body: JSON.stringify({ targetTotal, cycle }),
    }),

  updateCaixinhaBalance: (balance: number) =>
    request<{ message: string; balance: number }>(`/api/profile/caixinha`, {
      method: 'POST',
      body: JSON.stringify({ balance }),
    }),

  // Simulator
  simulate: (data: { amount: number; installments: number }) =>
    request<SimulatorResponse>(`/api/simulator`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  // Chat Quick-Command
  sendCommand: (message: string) =>
    request<ChatCommandResponse>(`/api/chat/command`, {
      method: 'POST',
      body: JSON.stringify({ message }),
    }),

  // Profile & Commitments
  getProfile: () =>
    request<{ profile: FinancialProfile; commitments: FixedCommitment[] }>(`/api/profile`),

  updateProfile: (profile: Partial<FinancialProfile>) =>
    request<{ message: string; profile: FinancialProfile }>(`/api/profile`, {
      method: 'PUT',
      body: JSON.stringify(profile),
    }),

  addCommitment: (commitment: Omit<FixedCommitment, 'id'>) =>
    request<FixedCommitment>(`/api/profile/commitments`, {
      method: 'POST',
      body: JSON.stringify(commitment),
    }),

  updateCommitment: (id: number, commitment: Partial<FixedCommitment>) =>
    request<{ message: string }>(`/api/profile/commitments/${id}`, {
      method: 'PUT',
      body: JSON.stringify(commitment),
    }),

  deleteCommitment: (id: number) =>
    request<{ message: string }>(`/api/profile/commitments/${id}`, {
      method: 'DELETE',
    }),
};
